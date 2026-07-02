import { CommonModule } from '@angular/common';
import { Component, HostListener } from '@angular/core';
import { FormGroup, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';

import { Gender, RegisterCommand } from '../../models/register-command.model';
import { AuthService } from '../../services/auth.service';

interface JalaliDay {
  day: number;
  label: string;
  isoDate: string;
  isToday: boolean;
  isSelected: boolean;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  readonly Gender = Gender;
  readonly weekDays = ['ش', 'ی', 'د', 'س', 'چ', 'پ', 'ج'];

  readonly genderOptions = [
    { label: 'آقا', value: Gender.Male },
    { label: 'خانم', value: Gender.Female }
  ];

  registerForm!: FormGroup;

  isSubmitting = false;
  isPasswordVisible = false;
  isDatepickerOpen = false;

  selectedJalaliDate = '';
  selectedAvatarName = '';
  selectedAvatarPreview = '';

  currentJalaliYear: number;
  currentJalaliMonth: number;
  calendarDays: Array<JalaliDay | null> = [];
  currentMonthTitle = '';

  private readonly jalaliFormatter = new Intl.DateTimeFormat('fa-IR-u-ca-persian', {
    year: 'numeric',
    month: 'numeric',
    day: 'numeric'
  });

  private readonly jalaliMonthFormatter = new Intl.DateTimeFormat('fa-IR-u-ca-persian', {
    year: 'numeric',
    month: 'long'
  });

  constructor(
    private readonly formBuilder: NonNullableFormBuilder,
    private readonly authService: AuthService,
    private readonly toastr: ToastrService
  ) {
    this.registerForm = this.formBuilder.group({
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      phoneNumber: ['', [Validators.required, Validators.pattern(/^09\d{9}$/)]],
      passwordHash: ['', [Validators.required, Validators.minLength(6)]],
      isCompleteProfile: [false],
      avatarImageName: [''],
      gender: [Gender.Male, [Validators.required]],
      birthDate: ['', [Validators.required]]
    });

    const today = this.getJalaliParts(new Date());
    this.currentJalaliYear = today.year;
    this.currentJalaliMonth = today.month;
    this.buildCalendar();
  }

  trackByGenderValue(_index: number, option: { value: Gender }): Gender {
    return option.value;
  }

  trackByText(_index: number, value: string): string {
    return value;
  }

  trackByCalendarDay(index: number, day: JalaliDay | null): string {
    return day?.isoDate ?? `empty-${index}`;
  }

  submit(): void {
    if (this.registerForm.invalid || this.isSubmitting) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;

    this.authService
      .register(this.createRegisterCommand())
      .pipe(finalize(() => (this.isSubmitting = false)))
      .subscribe({
        next: (result) => {
          if (result.isSuccess) {
            this.toastr.success(result.message);

            this.registerForm.reset({
              firstName: '',
              lastName: '',
              phoneNumber: '',
              passwordHash: '',
              isCompleteProfile: false,
              avatarImageName: '',
              gender: Gender.Male,
              birthDate: ''
            });

            this.selectedJalaliDate = '';
            this.selectedAvatarName = '';
            this.selectedAvatarPreview = '';
            this.buildCalendar();
            return;
          }

          this.toastr.error(result.message);
        },
        error: () => {
          this.toastr.error('خطا در ارتباط با سرور');
        }
      });
  }

  togglePasswordVisibility(): void {
    this.isPasswordVisible = !this.isPasswordVisible;
  }

  toggleDatepicker(event: MouseEvent): void {
    event.stopPropagation();
    this.isDatepickerOpen = !this.isDatepickerOpen;
  }

  previousMonth(): void {
    if (this.currentJalaliMonth === 1) {
      this.currentJalaliMonth = 12;
      this.currentJalaliYear -= 1;
    } else {
      this.currentJalaliMonth -= 1;
    }

    this.buildCalendar();
  }

  nextMonth(): void {
    if (this.currentJalaliMonth === 12) {
      this.currentJalaliMonth = 1;
      this.currentJalaliYear += 1;
    } else {
      this.currentJalaliMonth += 1;
    }

    this.buildCalendar();
  }

  selectDate(day: JalaliDay | null, event: MouseEvent): void {
    event.stopPropagation();

    if (!day) {
      return;
    }

    this.registerForm.controls['birthDate'].setValue(day.isoDate);
    this.registerForm.controls['birthDate'].markAsTouched();

    this.selectedJalaliDate = `${this.toPersianNumber(this.currentJalaliYear)}/${this.toPersianNumber(
      this.currentJalaliMonth.toString().padStart(2, '0')
    )}/${this.toPersianNumber(day.day.toString().padStart(2, '0'))}`;

    this.isDatepickerOpen = false;
    this.buildCalendar();
  }

  onAvatarSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) {
      this.selectedAvatarName = '';
      this.selectedAvatarPreview = '';
      this.registerForm.controls['avatarImageName'].setValue('');
      return;
    }

    this.selectedAvatarName = file.name;
    this.registerForm.controls['avatarImageName'].setValue(file.name);

    const reader = new FileReader();

    reader.onload = () => {
      this.selectedAvatarPreview = typeof reader.result === 'string' ? reader.result : '';
    };

    reader.readAsDataURL(file);
  }

  @HostListener('document:click')
  closeDatepicker(): void {
    this.isDatepickerOpen = false;
  }

  private createRegisterCommand(): RegisterCommand {
    const value = this.registerForm.getRawValue();

    return {
      firstName: value.firstName.trim(),
      lastName: value.lastName.trim(),
      phoneNumber: value.phoneNumber.trim(),
      passwordHash: value.passwordHash,
      isCompleteProfile: value.isCompleteProfile,
      avatarImageName: value.avatarImageName.trim() || null,
      gender: value.gender,
      birthDate: value.birthDate
    };
  }

  private buildCalendar(): void {
    const monthDates = this.getGregorianDatesOfJalaliMonth(
      this.currentJalaliYear,
      this.currentJalaliMonth
    );

    const firstDate = monthDates[0];
    const leadingEmptyDays = firstDate ? (firstDate.getDay() + 1) % 7 : 0;
    const todayIso = this.toIsoDate(new Date());
    const selectedIso = this.registerForm.controls['birthDate'].value;

    this.currentMonthTitle = firstDate
      ? this.jalaliMonthFormatter.format(firstDate)
      : `${this.currentJalaliYear}/${this.currentJalaliMonth}`;

    this.calendarDays = [
      ...Array<JalaliDay | null>(leadingEmptyDays).fill(null),
      ...monthDates.map((date) => {
        const parts = this.getJalaliParts(date);
        const isoDate = this.toIsoDate(date);

        return {
          day: parts.day,
          label: this.toPersianNumber(parts.day),
          isoDate,
          isToday: isoDate === todayIso,
          isSelected: isoDate === selectedIso
        };
      })
    ];
  }

  private getGregorianDatesOfJalaliMonth(year: number, month: number): Date[] {
    const dates: Date[] = [];
    const start = new Date(Date.UTC(year + 620, 0, 1));
    const end = new Date(Date.UTC(year + 622, 11, 31));

    for (
      let cursor = new Date(start);
      cursor <= end;
      cursor.setUTCDate(cursor.getUTCDate() + 1)
    ) {
      const localDate = new Date(
        cursor.getUTCFullYear(),
        cursor.getUTCMonth(),
        cursor.getUTCDate()
      );

      const parts = this.getJalaliParts(localDate);

      if (parts.year === year && parts.month === month) {
        dates.push(localDate);
      }
    }

    return dates;
  }

  private getJalaliParts(date: Date): { year: number; month: number; day: number } {
    const parts = this.jalaliFormatter.formatToParts(date);

    const getPart = (type: string): number =>
      Number(this.toEnglishNumber(parts.find((part) => part.type === type)?.value ?? '0'));

    return {
      year: getPart('year'),
      month: getPart('month'),
      day: getPart('day')
    };
  }

  private toIsoDate(date: Date): string {
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');

    return `${year}-${month}-${day}`;
  }

  private toPersianNumber(value: string | number): string {
    return value.toString().replace(/\d/g, (digit) => '۰۱۲۳۴۵۶۷۸۹'[Number(digit)]);
  }

  private toEnglishNumber(value: string): string {
    return value
      .replace(/[۰-۹]/g, (digit) => `${'۰۱۲۳۴۵۶۷۸۹'.indexOf(digit)}`)
      .replace(/[٠-٩]/g, (digit) => `${'٠١٢٣٤٥٦٧٨٩'.indexOf(digit)}`);
  }
}
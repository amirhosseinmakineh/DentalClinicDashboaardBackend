import { CommonModule } from '@angular/common';
import { Component, HostListener, OnInit, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterOutlet } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';

import { AdminRole, AdminUser, CreateUserCommandPayload, DeleteUserCommandPayload, UpdateUserCommandPayload } from '../../models/admin-management.model';
import { UserRole } from '../../models/auth.model';
import { Gender } from '../../models/register-command.model';
import { createPaginatedResult, PaginatedResult } from '../../models/paginated-result.model';
import { AuthSessionService } from '../../services/auth-session.service';
import { AdminManagementService } from '../../services/admin-management.service';
import { ConsultantDashboardComponent } from '../consultant-dashboard/consultant-dashboard.component';
import { DashboardHeaderComponent } from '../dashboard-header/dashboard-header.component';
import { RoleManagementComponent, RoleRow } from '../role-management/role-management.component';
import { SidebarComponent, SidebarItem } from '../sidebar/sidebar.component';
import { LeadManagmentComponent } from '../leadManagment/leadManagment.component';

interface DashboardStat {
  label: string;
  value: string;
  trend: string;
  icon: string;
}

interface UserRow {
  id: number | string;
  firstName: string;
  lastName: string;
  name: string;
  roleName: string;
  phoneNumber: string;
  isActive: boolean;
  isCompleteProfile: boolean;
  avatarImageName: string;
  gender: Gender;
  birthDate: string;
}

type DialogEntity = 'user';
type DialogMode = 'create' | 'edit' | 'delete';
type ExportFormat = 'excel' | 'pdf';

interface JalaliDay {
  day: number;
  label: string;
  isoDate: string;
  isToday: boolean;
  isSelected: boolean;
}

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, ReactiveFormsModule, SidebarComponent, DashboardHeaderComponent, RoleManagementComponent, ConsultantDashboardComponent, LeadManagmentComponent, RouterOutlet],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  isSidebarOpen = true;
  activeKey = 'overview';
  isDialogSubmitting = false;
  isUsersLoading = false;
  userLoadError = '';
  role: UserRole = 'unknown';

  userPageNumber = 1;
  readonly pageSizeOptions = [3, 5, 10];
  userPageSize = 3;
  readonly weekDays = ['ش', 'ی', 'د', 'س', 'چ', 'پ', 'ج'];
  isDatepickerOpen = false;
  selectedJalaliDate = '';
  selectedAvatarName = '';
  selectedAvatarPreview = '';
  currentJalaliYear: number;
  currentJalaliMonth: number;
  currentMonthTitle = '';
  calendarDays: Array<JalaliDay | null> = [];

  dialogEntity: DialogEntity | null = null;
  dialogMode: DialogMode | null = null;
  selectedUser: UserRow | null = null;

  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly adminManagementService = inject(AdminManagementService);
  private readonly authSession = inject(AuthSessionService);
  private readonly toastr = inject(ToastrService);
  private readonly router = inject(Router);

  private readonly jalaliFormatter = new Intl.DateTimeFormat('fa-IR-u-ca-persian', {
    year: 'numeric',
    month: 'numeric',
    day: 'numeric'
  });

  private readonly jalaliMonthFormatter = new Intl.DateTimeFormat('fa-IR-u-ca-persian', {
    year: 'numeric',
    month: 'long'
  });

  readonly userForm = this.formBuilder.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    phoneNumber: ['', [Validators.required, Validators.pattern(/^09\d{9}$/)]],
    passwordHash: ['', [Validators.minLength(6)]],
    roleName: ['', [Validators.required, Validators.maxLength(80)]],
    isActive: [false],
    isCompleteProfile: [false],
    avatarImageName: ['', [Validators.maxLength(200)]],
    gender: [Gender.Male, [Validators.required]],
    birthDate: ['']
  });

  private readonly adminSidebarItems: SidebarItem[] = [
    { key: 'overview', label: 'نمای کلی', icon: 'dashboard' },
    { key: 'users', label: 'مدیریت کاربران', icon: 'group' },
    { key: 'roles', label: 'مدیریت نقش ها', icon: 'admin_panel_settings' },
    { key: 'appointments', label: 'نوبت‌ها', icon: 'calendar_month' },
    { key: 'reports', label: 'گزارش‌ها', icon: 'monitoring' }
  ];


private readonly consultantSidebarItems: SidebarItem[] = [
  {
    key: 'consultant-dashboard',
    label: 'داشبورد مشاور',
    icon: 'dashboard',
    route: '/consultant/dashboard'
  },
  {
    key: 'consultant-leadManagment',
    label: 'مدیریت لید ها',
    icon: 'list',
    route: '/consultant/leadManagment'
  }
];
  readonly stats: DashboardStat[] = [
    { label: 'بیماران فعال', value: '۲,۴۸۰', trend: '+۱۲٪ نسبت به ماه قبل', icon: 'groups' },
    { label: 'نوبت‌های امروز', value: '۳۶', trend: '۸ نوبت در انتظار تایید', icon: 'event_available' },
    { label: 'درآمد ماه جاری', value: '۴۸۰م', trend: '+۱۸٪ رشد ماهانه', icon: 'payments' },
    { label: 'درمان‌های باز', value: '۱۲۴', trend: '۲۱ پرونده نیازمند پیگیری', icon: 'medical_services' }
  ];

  users: UserRow[] = [];

  roles: RoleRow[] = [];

  constructor() {
    const session = this.authSession.getSession();
    this.role = session?.role ?? 'unknown';
    const today = this.getJalaliParts(new Date());
    this.currentJalaliYear = today.year;
    this.currentJalaliMonth = today.month;
    this.activeKey = this.role === 'consultant' ? 'consultant-dashboard' : 'overview';
    this.buildCalendar();
  }

  ngOnInit(): void {
    this.loadRoles();
    this.loadUsers();
  }

  get sidebarItems(): SidebarItem[] {
    return this.role === 'consultant' ? this.consultantSidebarItems : this.adminSidebarItems;
  }

  get activeTitle(): string {
    return this.sidebarItems.find((item) => item.key === this.activeKey)?.label ?? 'داشبورد';
  }

  get activeSubtitle(): string {
    return this.role === 'consultant' ? 'دسترسی شما محدود به امکانات مشاور است.' : '';
  }

  get roleLabel(): string {
    if (this.role === 'admin') {
      return 'ادمین';
    }

    return 'کاربر';
  }

  get userPage(): PaginatedResult<UserRow> {
    return createPaginatedResult(this.users, this.userPageNumber, this.userPageSize);
  }

  get dialogTitle(): string {
    if (this.dialogEntity === 'user') {
      return this.dialogMode === 'create' ? 'افزودن کاربر جدید' : this.dialogMode === 'edit' ? 'ویرایش کاربر' : 'حذف کاربر';
    }

    return '';
  }

  toggleSidebar(): void {
    this.isSidebarOpen = !this.isSidebarOpen;
  }

  closeSidebar(): void {
    this.isSidebarOpen = false;
  }

setActive(item: SidebarItem): void {
  if (!this.sidebarItems.some((sidebarItem) => sidebarItem.key === item.key)) {
    return;
  }

  this.activeKey = item.key;

  if (item.route) {
    this.router.navigateByUrl(item.route);
  }
}

  logout(): void {
    this.authSession.clear();
    this.router.navigate(['/login']);
  }

  openUserDialog(mode: DialogMode, user?: UserRow): void {
    this.dialogEntity = 'user';
    this.dialogMode = mode;
    this.selectedUser = user ?? null;
    this.configureUserFormForMode(mode);

    if (mode === 'create') {
      this.userForm.reset({
        firstName: '',
        lastName: '',
        phoneNumber: '',
        passwordHash: '',
        roleName: this.roles[0]?.roleName ?? '',
        isActive: false,
        isCompleteProfile: false,
        avatarImageName: '',
        gender: Gender.Male,
        birthDate: ''
      });
      this.selectedAvatarName = '';
      this.selectedAvatarPreview = '';
      this.selectedJalaliDate = '';
      this.buildCalendar();
    } else if (user) {
      this.userForm.reset({
        firstName: user.firstName,
        lastName: user.lastName,
        phoneNumber: user.phoneNumber,
        passwordHash: '',
        roleName: user.roleName,
        isActive: user.isActive,
        isCompleteProfile: user.isCompleteProfile,
        avatarImageName: user.avatarImageName,
        gender: user.gender,
        birthDate: user.birthDate
      });
      this.selectedAvatarName = user.avatarImageName;
      this.selectedAvatarPreview = '';
      this.selectedJalaliDate = user.birthDate ? this.formatJalaliDisplayDate(user.birthDate) : '';
      this.buildCalendar();
    }
  }

  closeDialog(): void {
    this.dialogEntity = null;
    this.dialogMode = null;
    this.selectedUser = null;
    this.isDatepickerOpen = false;
    this.selectedAvatarName = '';
    this.selectedAvatarPreview = '';
    this.selectedJalaliDate = '';
  }

  submitDialog(): void {
    if (this.dialogEntity === 'user') {
      this.submitUserDialog();
    }
  }

  changeUserPage(pageNumber: number): void {
    this.userPageNumber = pageNumber;
  }

 changeUserPageSize(event: Event): void {
    this.userPageSize = Number((event.target as HTMLSelectElement).value);
    this.userPageNumber = 1;
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

    this.userForm.controls.birthDate.setValue(day.isoDate);
    this.userForm.controls.birthDate.markAsTouched();
    this.selectedJalaliDate = this.formatJalaliDisplayDate(day.isoDate);
    this.isDatepickerOpen = false;
    this.buildCalendar();
  }

  onAvatarSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) {
      this.selectedAvatarName = '';
      this.selectedAvatarPreview = '';
      this.userForm.controls.avatarImageName.setValue('');
      return;
    }

    this.selectedAvatarName = file.name;
    this.userForm.controls.avatarImageName.setValue(file.name);
    this.userForm.controls.avatarImageName.markAsTouched();

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

  exportUsers(format: ExportFormat): void {
    this.exportRows(
      format,
      'users',
      ['کاربر', 'نقش', 'شماره تماس', 'وضعیت', 'تکمیل پروفایل'],
      this.users.map((user) => [user.name, user.roleName, user.phoneNumber, this.getUserStatusLabel(user), this.getProfileStatusLabel(user)])
    );
  }

  trackById(_index: number, item: { id: number | string }): number | string {
    return item.id;
  }

  trackByLabel(_index: number, item: { label: string }): string {
    return item.label;
  }

  trackByValue(_index: number, value: number | string): number | string {
    return value;
  }

  trackByCalendarDay(index: number, day: JalaliDay | null): string {
    return day?.isoDate ?? `empty-${index}`;
  }

  private submitUserDialog(): void {
    if (this.dialogMode !== 'delete' && this.userForm.invalid) {
      this.userForm.markAllAsTouched();
      return;
    }

    if (this.isDialogSubmitting) {
      return;
    }

    if (this.dialogMode === 'delete' && this.selectedUser) {
      this.isDialogSubmitting = true;
      const command = this.toDeleteUserCommand(this.selectedUser);

      this.adminManagementService
        .deleteUser(command)
        .pipe(finalize(() => (this.isDialogSubmitting = false)))
        .subscribe((result) => {
          if (!result.isSuccess) {
            this.toastr.error(result.message || 'حذف کاربر ناموفق بود.');
            return;
          }

          this.users = this.users.filter((user) => user.id !== this.selectedUser?.id);
            this.userPageNumber = Math.min(this.userPageNumber, Math.max(1, Math.ceil(this.users.length / this.userPageSize)));
          this.toastr.success(result.message || 'کاربر حذف شد.');
          this.closeDialog();
        });
      return;
    }

    const value = this.userForm.getRawValue();
    const nextUser: UserRow = {
      id: this.selectedUser?.id ?? this.getNextId(this.users),
      firstName: value.firstName.trim(),
      lastName: value.lastName.trim(),
      name: [value.firstName.trim(), value.lastName.trim()].filter(Boolean).join(' '),
      roleName: value.roleName.trim(),
      phoneNumber: value.phoneNumber.trim(),
      isActive: value.isActive,
      isCompleteProfile: value.isCompleteProfile,
      avatarImageName: value.avatarImageName.trim(),
      gender: value.gender,
      birthDate: value.birthDate
    };
    const request$ = this.dialogMode === 'edit'
      ? this.adminManagementService.updateUser(this.toUpdateUserCommand(nextUser))
      : this.adminManagementService.createUser(this.toCreateUserCommand(nextUser, value.passwordHash.trim()));

    this.isDialogSubmitting = true;
    request$
      .pipe(finalize(() => (this.isDialogSubmitting = false)))
      .subscribe((result) => {
        if (!result.isSuccess) {
          this.toastr.error(result.message || 'ثبت اطلاعات کاربر ناموفق بود.');
          return;
        }

        this.users = this.dialogMode === 'edit' ? this.users.map((user) => (user.id === nextUser.id ? nextUser : user)) : [nextUser, ...this.users];
        this.userPageNumber = 1;
        this.toastr.success(result.message || (this.dialogMode === 'edit' ? 'کاربر به‌روزرسانی شد.' : 'کاربر جدید ایجاد شد.'));
        this.closeDialog();
        this.loadUsers(false);
      });
  }

  private loadUsers(showLoading = true): void {
    if (showLoading) {
      this.isUsersLoading = true;
    }

    this.adminManagementService
      .getUsers()
      .pipe(finalize(() => (this.isUsersLoading = false)))
      .subscribe((result) => {
        if (!result.isSuccess) {
          this.userLoadError = result.message || 'امکان دریافت کاربران از API پورت ۵۱۸۲ وجود ندارد.';
          this.toastr.error(this.userLoadError);
          return;
        }

        this.userLoadError = '';
        this.users = (result.data ?? []).map((user, index) => this.toUserRow(user, index));
        this.userPageNumber = 1;
      });
  }

  private toUserRow(user: AdminUser, index: number): UserRow {
    const firstName = user.firstName?.trim() ?? '';
    const lastName = user.lastName?.trim() ?? '';
    const fullName = user.fullName ?? user.name ?? [firstName, lastName].filter(Boolean).join(' ');

    return {
      id: user.id ?? user.userId ?? index + 1,
      firstName: firstName || this.getNamePart(fullName, 'first'),
      lastName: lastName || this.getNamePart(fullName, 'last'),
      name: fullName || 'کاربر بدون نام',
      roleName: user.roleName ?? user.role ?? 'بدون نقش',
      phoneNumber: user.phoneNumber ?? user.phone ?? '-',
      isActive: user.isActive ?? this.statusToIsActive(user.status),
      isCompleteProfile: user.isCompleteProfile ?? false,
      avatarImageName: user.avatarImageName ?? '',
      gender: this.normalizeGender(user.gender),
      birthDate: user.birthDate ?? ''
    };
  }

  private toCreateUserCommand(user: UserRow, passwordHash: string): CreateUserCommandPayload {
    return {
      firstName: user.firstName,
      lastName: user.lastName,
      phoneNumber: user.phoneNumber,
      passwordHash,
      birthDate: user.birthDate,
      gender: user.gender,
      avatarImageName: user.avatarImageName || null,
      roleName: user.roleName
    };
  }

  private toUpdateUserCommand(user: UserRow): UpdateUserCommandPayload {
    return {
      id: user.id,
      firstName: user.firstName,
      lastName: user.lastName,
      phoneNumber: user.phoneNumber,
      isCompleteProfile: user.isCompleteProfile,
      avatarImageName: user.avatarImageName || null,
      gender: user.gender,
      isActive: user.isActive,
      roleName: user.roleName
    };
  }

  private toDeleteUserCommand(user: UserRow): DeleteUserCommandPayload {
    return { userId: user.id };
  }

  getUserStatusLabel(user: UserRow): string {
    return user.isActive ? 'فعال' : 'غیرفعال';
  }

  getProfileStatusLabel(user: UserRow): string {
    return user.isCompleteProfile ? 'تکمیل شده' : 'ناقص';
  }

  getGenderLabel(gender: Gender): string {
    return gender === Gender.Female ? 'خانم' : 'آقا';
  }

  private configureUserFormForMode(mode: DialogMode): void {
    const passwordControl = this.userForm.controls.passwordHash;
    const birthDateControl = this.userForm.controls.birthDate;

    if (mode === 'create') {
      passwordControl.setValidators([Validators.required, Validators.minLength(6)]);
      birthDateControl.setValidators([Validators.required]);
    } else {
      passwordControl.setValidators([Validators.minLength(6)]);
      birthDateControl.clearValidators();
    }

    passwordControl.updateValueAndValidity();
    birthDateControl.updateValueAndValidity();
  }

  updateRoleOptions(roles: RoleRow[]): void {
    this.roles = roles;
  }

  private loadRoles(): void {
    this.adminManagementService.getRoles().subscribe((result) => {
      if (!result.isSuccess) {
        this.toastr.error(result.message || 'امکان دریافت نقش‌ها از API پورت ۵۱۸۲ وجود ندارد.');
        this.roles = [];
        return;
      }

      this.roles = (result.data ?? []).map((role, index) => this.toRoleRow(role, index));
    });
  }

  private toRoleRow(role: AdminRole, index: number): RoleRow {
    return {
      id: role.id ?? role.roleId ?? index + 1,
      roleName: role.roleName?.trim() || 'نقش بدون نام'
    };
  }

  private getNamePart(fullName: string | null | undefined, part: 'first' | 'last'): string {
    const parts = `${fullName ?? ''}`.trim().split(/\s+/).filter(Boolean);
    return part === 'first' ? parts[0] ?? '' : parts.slice(1).join(' ');
  }

  private statusToIsActive(status: string | null | undefined): boolean {
    return !status || ['فعال', 'active', 'true'].includes(`${status}`.trim().toLowerCase());
  }

  private normalizeGender(gender: Gender | number | null | undefined): Gender {
    return Number(gender) === Gender.Female ? Gender.Female : Gender.Male;
  }

  private buildCalendar(): void {
    const monthDates = this.getGregorianDatesOfJalaliMonth(
      this.currentJalaliYear,
      this.currentJalaliMonth
    );
    const firstDate = monthDates[0];
    const leadingEmptyDays = firstDate ? (firstDate.getDay() + 1) % 7 : 0;
    const todayIso = this.toIsoDate(new Date());
    const selectedIso = this.userForm.controls.birthDate.value;

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

    for (let cursor = new Date(start); cursor <= end; cursor.setUTCDate(cursor.getUTCDate() + 1)) {
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

  private formatJalaliDisplayDate(isoDate: string): string {
    const date = new Date(`${isoDate}T00:00:00`);
    const parts = this.getJalaliParts(date);

    return `${this.toPersianNumber(parts.year)}/${this.toPersianNumber(parts.month.toString().padStart(2, '0'))}/${this.toPersianNumber(
      parts.day.toString().padStart(2, '0')
    )}`;
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

  private getNextId(rows: Array<{ id: number | string }>): number {
    const numericIds = rows.map((row) => Number(row.id)).filter((id) => Number.isFinite(id));
    return numericIds.length ? Math.max(...numericIds) + 1 : 1;
  }

  private exportRows(format: ExportFormat, fileName: string, headers: string[], rows: string[][]): void {
    if (format === 'excel') {
      const csv = [headers, ...rows].map((row) => row.map((cell) => `"${cell.replace(/"/g, '""')}"`).join(',')).join('\n');
      this.downloadFile(`${fileName}.csv`, `\ufeff${csv}`, 'text/csv;charset=utf-8;');
      return;
    }

    const tableRows = rows.map((row) => `<tr>${row.map((cell) => `<td>${this.escapeHtml(cell)}</td>`).join('')}</tr>`).join('');
    const html = `<!doctype html><html lang="fa" dir="rtl"><head><meta charset="utf-8"><title>${fileName}</title><style>body{font-family:tahoma,sans-serif;padding:24px}table{width:100%;border-collapse:collapse}th,td{border:1px solid #ddd;padding:10px;text-align:right}</style></head><body><h1>${fileName}</h1><table><thead><tr>${headers.map((header) => `<th>${this.escapeHtml(header)}</th>`).join('')}</tr></thead><tbody>${tableRows}</tbody></table></body></html>`;
    const printWindow = window.open('', '_blank', 'noopener,noreferrer');
    printWindow?.document.write(html);
    printWindow?.document.close();
    printWindow?.print();
  }

  private escapeHtml(value: string): string {
    return value
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/\"/g, '&quot;')
      .replace(/'/g, '&#039;');
  }

  private downloadFile(fileName: string, content: string, type: string): void {
    const blob = new Blob([content], { type });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
  }
}

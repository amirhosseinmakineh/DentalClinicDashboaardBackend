import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';

import { AuthSessionService } from '../../services/auth-session.service';
import { ConsultantProfileService } from '../../services/consultant-profile.service';

@Component({
  selector: 'app-consultant-profile-completion',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './consultant-profile-completion.component.html',
  styleUrl: './consultant-profile-completion.component.scss'
})
export class ConsultantProfileCompletionComponent {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly consultantProfileService = inject(ConsultantProfileService);
  private readonly authSession = inject(AuthSessionService);
  private readonly router = inject(Router);
  private readonly toastr = inject(ToastrService);

  isSubmitting = false;

  readonly profileForm = this.formBuilder.group({
    nationalCode: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
    address: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]]
  });

  submitProfile(): void {
    if (this.profileForm.invalid || this.isSubmitting) {
      this.profileForm.markAllAsTouched();
      return;
    }

    const value = this.profileForm.getRawValue();
    this.isSubmitting = true;

    this.consultantProfileService
      .completeProfile({
        NationalityCode: value.nationalCode.trim(),
        address: value.address.trim()
      })
      .pipe(finalize(() => (this.isSubmitting = false)))
      .subscribe((result) => {
        if (!result.isSuccess) {
          this.toastr.error(result.message || 'تکمیل پروفایل ناموفق بود.');
          return;
        }

        this.authSession.markProfileCompleted(this.getCompletedProfileId(result.data));
        this.toastr.success(result.message || 'پروفایل با موفقیت تکمیل شد.');
        void this.router.navigate(['/consultant/dashboard']);
      });
  }
  private getCompletedProfileId(data: unknown): number | undefined {
    if (typeof data === 'number' || typeof data === 'string') {
      return this.toPositiveNumber(data);
    }

    if (!data || typeof data !== 'object') {
      return undefined;
    }

    const response = data as Record<string, unknown>;
    const value = response['profileId'] ?? response['ProfileId'] ?? response['id'] ?? response['Id'] ?? response['consultantProfileId'] ?? response['ConsultantProfileId'];

    return this.toPositiveNumber(value);
  }

  private toPositiveNumber(value: unknown): number | undefined {
    const numericValue = Number(value);
    return Number.isFinite(numericValue) && numericValue > 0 ? numericValue : undefined;
  }

}

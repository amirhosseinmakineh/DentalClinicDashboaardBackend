import { Component } from '@angular/core';
import { FormGroup, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';

import { AuthService } from '../../services/auth.service';
import { AuthSession } from '../../models/auth.model';
import { AuthSessionService } from '../../services/auth-session.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  loginForm!: FormGroup;

  isPasswordVisible = false;
  isSubmitting = false;

  constructor(
    private readonly formBuilder: NonNullableFormBuilder,
    private readonly authService: AuthService,
    private readonly authSession: AuthSessionService,
    private readonly router: Router,
    private readonly toastr: ToastrService
  ) {
    this.loginForm = this.formBuilder.group({
      phoneNumber: ['', [Validators.required]],
      passwordHash: ['', [Validators.required]]
    });
  }

  submit(): void {
    if (this.loginForm.invalid || this.isSubmitting) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const value = this.loginForm.getRawValue();

    this.authService
      .login({
        phoneNumber: value.phoneNumber.trim(),
        passwordHash: value.passwordHash
      })
      .pipe(finalize(() => (this.isSubmitting = false)))
      .subscribe({
        next: (result) => {
          if (!result.isSuccess) {
            this.toastr.error(result.message);
            return;
          }

          const token = this.getToken(result.data);

          if (!token) {
            this.toastr.error(result.message || 'توکن ورود از سمت سرور دریافت نشد');
            return;
          }

          const session = this.authSession.setToken(token);

          this.toastr.success(result.message);
          void this.router.navigate([this.getRedirectUrl(session)]);
        },
        error: () => {
          this.toastr.error('خطا در ارتباط با سرور');
        }
      });
  }

  togglePasswordVisibility(): void {
    this.isPasswordVisible = !this.isPasswordVisible;
  }

  private getRedirectUrl(session: AuthSession): string {
    if (session.role === 'admin') {
      return '/admin';
    }

    return session.role === 'consultant' && !session.isCompleteProfile ? '/consultant/complete-profile' : '/consultant/dashboard';
  }

  private getToken(data: unknown): string | null {
    if (!data || typeof data !== 'object') {
      return null;
    }

    const response = data as Record<string, unknown>;

    const token =
      response['token'] ??
      response['accessToken'] ??
      response['jwtToken'] ??
      response['jwt'];

    return typeof token === 'string' && token.trim() ? token : null;
  }
}
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { AuthService } from '../../../core/services';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    NzInputModule,
    NzButtonModule,
    NzIconModule,
    NzFormModule,
    NzAlertModule,
  ],
  template: `
    <div class="login">
      <div class="login__mark">
        <span class="login__mark-dot"></span>
        <h1>Atas</h1>
        <p>Registro das reuniões da ala</p>
      </div>

      <form [formGroup]="form" (ngSubmit)="submit()" class="login__form" nz-form>
        @if (error()) {
          <nz-alert nzType="error" [nzMessage]="error()" nzShowIcon class="login__error"></nz-alert>
        }

        <label class="login__label">Usuário (ala)</label>
        <nz-input-group nzPrefixIcon="user" nzSize="large">
          <input nz-input formControlName="username" placeholder="Nome da ala" autocomplete="username" />
        </nz-input-group>

        <label class="login__label">Senha</label>
        <nz-input-group nzPrefixIcon="lock" nzSize="large">
          <input
            nz-input
            type="password"
            formControlName="password"
            placeholder="••••••••"
            autocomplete="current-password"
          />
        </nz-input-group>

        <button
          nz-button
          nzType="primary"
          nzSize="large"
          nzBlock
          type="submit"
          class="login__submit"
          [nzLoading]="loading()"
          [disabled]="form.invalid"
        >
          Entrar
        </button>
      </form>
    </div>
  `,
  styles: [
    `
      .login {
        min-height: 100dvh;
        display: flex;
        flex-direction: column;
        justify-content: center;
        padding: 32px 24px calc(32px + var(--safe-bottom));
      }
      .login__mark {
        text-align: center;
        margin-bottom: 40px;
      }
      .login__mark-dot {
        display: inline-block;
        width: 14px;
        height: 14px;
        border-radius: 50%;
        background: linear-gradient(135deg, var(--accent-sacramental), var(--accent-batismo));
        margin-bottom: 14px;
      }
      .login__mark h1 {
        font-size: 30px;
        margin-bottom: 4px;
      }
      .login__mark p {
        margin: 0;
        color: var(--ink-soft);
        font-size: 14px;
      }
      .login__form {
        display: flex;
        flex-direction: column;
        gap: 6px;
      }
      .login__label {
        font-size: 13px;
        font-weight: 600;
        color: var(--ink-soft);
        margin: 10px 0 2px;
      }
      .login__submit {
        margin-top: 20px;
        height: 50px;
        font-size: 16px;
        border-radius: var(--radius);
      }
      .login__error {
        margin-bottom: 4px;
      }
    `,
  ],
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  form = this.fb.group({
    username: ['', Validators.required],
    password: ['', Validators.required],
  });

  loading = signal(false);
  error = signal<string | null>(null);

  submit(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);

    const { username, password } = this.form.getRawValue();
    this.auth.login({ username: username!, password: password! }).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigate(['/atas']);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Usuário ou senha inválidos.');
      },
    });
  }
}

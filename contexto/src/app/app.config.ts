import { ApplicationConfig, importProvidersFrom, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { NZ_I18N, pt_BR } from 'ng-zorro-antd/i18n';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { LOCALE_ID } from '@angular/core';

import {
  PlusOutline,
  DeleteOutline,
  EditOutline,
  CheckCircleOutline,
  CalendarOutline,
  UserOutline,
  LockOutline,
  LogoutOutline,
  ArrowLeftOutline,
  MoreOutline,
  FileTextOutline,
  DropboxOutline,
  CloseCircleFill,
  ExclamationCircleOutline,
  EyeOutline,
} from '@ant-design/icons-angular/icons';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAnimationsAsync(),
    { provide: LOCALE_ID, useValue: 'pt-BR' },
    { provide: NZ_I18N, useValue: pt_BR },
    importProvidersFrom(
      NzIconModule.forRoot([
        PlusOutline,
        DeleteOutline,
        EditOutline,
        CheckCircleOutline,
        CalendarOutline,
        UserOutline,
        LockOutline,
        LogoutOutline,
        ArrowLeftOutline,
        MoreOutline,
        FileTextOutline,
        DropboxOutline,
        CloseCircleFill,
        ExclamationCircleOutline,
        EyeOutline,
      ])
    ),
  ],
};

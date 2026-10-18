import { mergeApplicationConfig, ApplicationConfig } from '@angular/core';
import { provideServerRendering, withRoutes } from '@angular/ssr';
import { appConfig } from './app.config';
import { serverRoutes } from './app.routes.server';
import { API_ORIGIN } from '@shared/http/api-origin.token';
const serverConfig: ApplicationConfig = {
  providers: [
    provideServerRendering(withRoutes(serverRoutes)),
    {
      provide: API_ORIGIN,
      useFactory: () => process.env['STORE_API_URL'] || 'http://127.0.0.1:5099',
    },
  ],
};
export const config = mergeApplicationConfig(appConfig, serverConfig);

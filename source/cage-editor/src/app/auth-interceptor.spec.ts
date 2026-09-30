import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { vi, type Mock } from 'vitest';

import { authInterceptor } from './auth-interceptor';
import { AuthService } from './auth-service';
import { environment } from '../environments/environment';

const FAKE_TOKEN = 'test' + '-token';
const AUTH_SCHEME = 'Bear' + 'er';

function flushMicrotasks(): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, 0));
}

describe('authInterceptor', () => {
  let httpClient: HttpClient;
  let httpTestingController: HttpTestingController;
  let getAccessTokenSpy: Mock;

  beforeEach(() => {
    getAccessTokenSpy = vi.fn().mockResolvedValue(FAKE_TOKEN);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        {
          provide: AuthService,
          useValue: {
            getAccessToken: getAccessTokenSpy,
          },
        },
      ],
    });

    httpClient = TestBed.inject(HttpClient);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('attaches the auth scheme and token as an Authorization header for backend requests', async () => {
    const requestPromise = firstValueFrom(httpClient.get(environment.backendUrl + '/organizations'));

    await flushMicrotasks();

    const request = httpTestingController.expectOne(environment.backendUrl + '/organizations');
    expect(request.request.headers.get('Authorization')).toBe(`${AUTH_SCHEME} ${FAKE_TOKEN}`);
    request.flush([]);

    await requestPromise;
    expect(getAccessTokenSpy).toHaveBeenCalled();
  });

  it('does not attach an Authorization header for requests to other origins', async () => {
    const requestPromise = firstValueFrom(httpClient.get('https://example.com/other'));

    const request = httpTestingController.expectOne('https://example.com/other');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});

    await requestPromise;
    expect(getAccessTokenSpy).not.toHaveBeenCalled();
  });

  it('does not attach an Authorization header when no token is available', async () => {
    getAccessTokenSpy.mockResolvedValue(null);

    const requestPromise = firstValueFrom(httpClient.get(environment.backendUrl + '/organizations'));

    await flushMicrotasks();

    const request = httpTestingController.expectOne(environment.backendUrl + '/organizations');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush([]);

    await requestPromise;
  });
});
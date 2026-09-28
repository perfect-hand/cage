import {
  computed,
  Service,
  signal,
} from '@angular/core';
import {
  AccountInfo,
  AuthenticationResult,
  Configuration,
  InteractionRequiredAuthError,
  LogLevel,
  PublicClientApplication,
} from '@azure/msal-browser';

@Service()
export class AuthService {
  private msalInstance!: PublicClientApplication;

  private readonly _accountId = signal('');
  private readonly _userName = signal('');
  private readonly _accessToken = signal('');

  readonly userName = this._userName.asReadonly();
  readonly isSignedIn = computed(() => this._userName() !== '');

  async initialize(): Promise<void> {
    /**
     * Configuration object to be passed to MSAL instance on creation.
     * For a full list of MSAL.js configuration parameters, visit:
     * https://github.com/AzureAD/microsoft-authentication-library-for-js/blob/dev/lib/msal-browser/docs/configuration.md
     */
    const msalConfig: Configuration = {
      auth: {
        clientId: '7b47f7a5-835d-4623-90b5-9d05ee36c6fe', // Frontend application client ID
        authority: 'https://perfecthandcage.ciamlogin.com/81875ece-550a-4647-8561-ef29633bfe62',
        redirectUri: '/', // You must register this URI on App Registration. Defaults to window.location.href e.g. http://localhost:3000/
      },
      cache: {
        cacheLocation: 'sessionStorage', // Configures cache location. "sessionStorage" is more secure, but "localStorage" gives you SSO.
      },
      system: {
        loggerOptions: {
          loggerCallback: (level, message, containsPii) => {
            if (containsPii) {
              return;
            }
            switch (level) {
              case LogLevel.Error:
                console.error(message);
                return;
              case LogLevel.Info:
                console.info(message);
                return;
              case LogLevel.Verbose:
                console.debug(message);
                return;
              case LogLevel.Warning:
                console.warn(message);
                return;
            }
          },
        },
      },
    };

    this.msalInstance = new PublicClientApplication(msalConfig);

    await this.msalInstance.initialize();

    try {
      const result = await this.msalInstance.handleRedirectPromise();
      this.handleAuthenticationResult(result);
    } catch (error) {
      console.error(error);
    }
  }

  getAccessToken(): string {
    return this._accessToken();
  }

  private handleAuthenticationResult(result: AuthenticationResult | null): void {
    if (result !== null) {
      this._accountId.set(result.account.homeAccountId);
      this.msalInstance.setActiveAccount(result.account);
      this.showWelcomeMessageAndAcquireToken(result.account);
      return;
    }

    this.selectAccount();
  }

  private selectAccount(): void {
    /**
     * See here for more info on account retrieval:
     * https://github.com/AzureAD/microsoft-authentication-library-for-js/blob/dev/lib/msal-common/docs/Accounts.md
     */
    const currentAccounts = this.msalInstance.getAllAccounts();

    if (!currentAccounts || currentAccounts.length < 1) {
      return;
    }

    this.showWelcomeMessageAndAcquireToken(currentAccounts[0]);
  }

  private showWelcomeMessageAndAcquireToken(account: AccountInfo): void {
    this._userName.set(account.username);

    const request = {
      scopes: ['api://a6791fa3-10da-4339-8097-ba31b7245e02/user_impersonation'],
    };

    this.msalInstance
      .acquireTokenSilent(request)
      .then((tokenResponse) => {
        this._accessToken.set(tokenResponse.accessToken);
      })
      .catch((error) => {
        if (error instanceof InteractionRequiredAuthError) {
          void this.msalInstance.acquireTokenRedirect(request);
        }
      });
  }

  async signIn(): Promise<void> {
    /**
     * Scopes you add here will be prompted for user consent during sign-in.
     * By default, MSAL.js will add OIDC scopes (openid, profile, email) to any login request.
     * For more information about OIDC scopes, visit:
     * https://learn.microsoft.com/entra/identity-platform/permissions-consent-overview#openid-connect-scopes
     */
    const loginRequest = {
      scopes: ['api://a6791fa3-10da-4339-8097-ba31b7245e02/user_impersonation'],
    };

    await this.msalInstance.loginRedirect(loginRequest);
  }

  async signOut(): Promise<void> {
    const currentAccount = this.msalInstance.getAccount({
      homeAccountId: this._accountId(),
    });

    await this.msalInstance.logoutRedirect({
      account: currentAccount ?? undefined,
    });
  }
}

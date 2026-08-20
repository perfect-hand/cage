import { HttpClient } from '@angular/common/http';
import { Component, signal, computed, OnInit, inject } from '@angular/core';
import { PublicClientApplication, LogLevel, Configuration, AuthenticationResult, AccountInfo, InteractionRequiredAuthError } from '@azure/msal-browser';

@Component({
  selector: 'login',
  imports: [],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login implements OnInit {

  private msalInstance!: PublicClientApplication;
  private accountId = "";

  private http = inject(HttpClient);

  protected userName = signal('');
  protected isSignedIn = computed(() => this.userName() != '');

  ngOnInit() {
    /**
    * Configuration object to be passed to MSAL instance on creation. 
    * For a full list of MSAL.js configuration parameters, visit:
    * https://github.com/AzureAD/microsoft-authentication-library-for-js/blob/dev/lib/msal-browser/docs/configuration.md 
    */
    const msalConfig: Configuration = {
      auth: {
        clientId: "7b47f7a5-835d-4623-90b5-9d05ee36c6fe", // Frontend application client ID
        authority: "https://perfecthandcage.ciamlogin.com/81875ece-550a-4647-8561-ef29633bfe62",
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
    this.msalInstance.initialize().then(() => {
      // Redirect: once login is successful and redirects with tokens, call Graph API
      this.msalInstance.handleRedirectPromise()
        .then(result => this.handleAuthenticationResult(result))
        .catch(error => console.error(error));
    })
  }

  private handleAuthenticationResult(result: AuthenticationResult | null) {
    if (result !== null) {
      this.accountId = result.account.homeAccountId;
      this.msalInstance.setActiveAccount(result.account);
      this.showWelcomeMessage(result.account);
    } else {
      this.selectAccount();
    }
  }

  private selectAccount() {
    /**
    * See here for more info on account retrieval: 
    * https://github.com/AzureAD/microsoft-authentication-library-for-js/blob/dev/lib/msal-common/docs/Accounts.md
    */
    const currentAccounts = this.msalInstance.getAllAccounts();

    if (!currentAccounts || currentAccounts.length < 1) {
      return;
    }

    this.showWelcomeMessage(currentAccounts[0]);
  }

  private showWelcomeMessage(account: AccountInfo) {
    this.userName.set(account.username);
  };

  protected async signIn() {
    /**
    * Scopes you add here will be prompted for user consent during sign-in.
    * By default, MSAL.js will add OIDC scopes (openid, profile, email) to any login request.
    * For more information about OIDC scopes, visit: 
    * https://learn.microsoft.com/entra/identity-platform/permissions-consent-overview#openid-connect-scopes
    */
    const loginRequest = {
      scopes: ["User.Read"],
    };

    await this.msalInstance.loginRedirect(loginRequest)
  }

  protected async signOut() {
    const currentAccount = this.msalInstance.getAccount({ homeAccountId: this.accountId });

    await this.msalInstance.logoutRedirect({
      account: currentAccount
    });
  }

  protected async sendRequest() {
    var request = {
      scopes: ["api://a6791fa3-10da-4339-8097-ba31b7245e02/user_impersonation"],
    };

    this.msalInstance.acquireTokenSilent(request).then(tokenResponse => {
      console.log(tokenResponse);

      this.http.get('https://cage-simulation.azurewebsites.net/api/HttpExample', {
        headers: {
          'Authorization': "Bearer " + tokenResponse.accessToken,
        },
        responseType: 'text'
      }).subscribe((response) => {
        console.log(response);
      });
    }).catch(error => {
      if (error instanceof InteractionRequiredAuthError) {
        // fallback to interaction when silent call fails
        this.msalInstance.acquireTokenRedirect(request)
      }

      // handle other errors
    });
  }
}

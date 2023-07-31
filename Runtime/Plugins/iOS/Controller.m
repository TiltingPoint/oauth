#import <Foundation/Foundation.h>
#import "AppAuth.h"
#import "Controller.h"

@implementation Controller 

static OIDServiceConfiguration *_configuration = nil;
static NSString *_clientId;
static NSURL *_callbackUrl;
static char *_TpMessageReceiver = "TPAuth";
static NSString *const kAuthStateKey = @"authState";
static NSString *const suitName = @"com.tiltingpoint.auth";
static NSString *const defaultGrantType = @"urn:ietf:params:oauth:grant-type:token-exchange";
static NSString *const defaultJwtTokenType = @"urn:ietf:params:oauth:token-type:jwt";

+ (Controller *)instance {
    static Controller *sharedClass = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        sharedClass = [[self alloc] init];
    });
    return sharedClass; 
}

+ (void)Initialize:(char *)issuerChar :(char *)clientIdChar :(char *)callbackUrlChar {
    NSString *issuer = [NSString stringWithFormat:@"%s", issuerChar];
    NSString *clientId = [NSString stringWithFormat:@"%s", clientIdChar];
    NSString *callbackUrl = [NSString stringWithFormat:@"%s", callbackUrlChar];
    
    NSURL *issuerUrl = [NSURL URLWithString:issuer];
    
    [[self instance] loadState];
    
    [OIDAuthorizationService discoverServiceConfigurationForIssuer:issuerUrl completion:^(OIDServiceConfiguration *_Nullable configuration, NSError *_Nullable error) {
        if (!configuration) {
            NSLog(@"TiltingPointAuth: Error retrieving discovery document: %@", [error localizedDescription]);
            [[self instance] UnityMessage:"InitDidFail" stringMsg:[error localizedDescription]];
            return;
        }
        
        _configuration = configuration;
        _clientId = clientId;
        _callbackUrl = [NSURL URLWithString:[NSString stringWithFormat:@"%@", callbackUrl]];
        
        NSString *tokenEndpoint = configuration.tokenEndpoint.absoluteString;
        [[self instance] UnityMessage:"InitDidSucceed"];
    }];
}

+ (void)Authenticate {
    [[self instance] UnityMessage:"AuthWillStart"];
    
    OIDAuthorizationRequest *request = [[OIDAuthorizationRequest alloc] initWithConfiguration:_configuration
                                                                                     clientId:_clientId
                                                                                       scopes:@[OIDScopeOpenID, OIDScopeProfile]
                                                                                  redirectURL:_callbackUrl
                                                                                 responseType:OIDResponseTypeCode
                                                                         additionalParameters:nil];
    
    UIViewController *controller = [self instance].GetRootViewController;
    
    [self instance]->_currentAuthorizationFlow = [OIDAuthState authStateByPresentingAuthorizationRequest:request presentingViewController:controller callback:^(OIDAuthState *_Nullable authState, NSError *_Nullable error) {
        if (authState) {
            [[self instance] setAuthState:authState];
            [[self instance] saveState];
            [[self instance] UnityMessage:"AuthDidSucceed" stringMsg:[[self instance] GetStringifiedStateTokens]];
        } else {
            NSLog(@"[TP AUTH] Authorization error: %@", [error localizedDescription]);
            [[self instance] UnityMessage:"AuthDidFail" stringMsg:[error localizedDescription]];
        }
    }];
}

+ (void)GetTokens {
    if ([[self instance] authState] != nil) {
        [[[self instance] authState] performActionWithFreshTokens:^(NSString *_Nonnull accessToken, NSString *_Nonnull idToken, NSError *_Nullable error) {
            if (error) {
                NSLog(@"[TP AUTH] Error fetching fresh tokens: %@", [error localizedDescription]);
                [[self instance] UnityMessage:"TokenRequestDidFail" stringMsg:[error localizedDescription]];
                return;
            }
            NSLog(@"[TP AUTH] Successfully fetched fresh tokens.");
            NSDictionary *stateTokens = [[self instance] GetStateTokens];
            NSDictionary *freshTokens = @{
                @"AccessToken": accessToken,
                @"IdToken": idToken,
                @"RefreshToken": [stateTokens valueForKey:@"RefreshToken"]
            };
            [[self instance] UnityMessage:"TokenRequestDidSucceed" stringMsg:[[self instance] DictToJson:freshTokens]];
        }];
    } else {
        [[self instance] UnityMessage:"TokenRequestDidFail" stringMsg:@"Please authenticate your user first."];
    }
}

+ (void)TokenExchange:(char *)tokenChar :(char *)issuerChar {
    NSString *token = [NSString stringWithFormat:@"%s", tokenChar];
    NSString *issuer = [NSString stringWithFormat:@"%s", issuerChar];
    NSDictionary<NSString *, NSString *> *additionalParams = @{
        @"subject_token": token,
        @"subject_issuer": issuer
    };
    
    if ([issuer isEqualToString:@"apple"]) {
        NSMutableDictionary *tempDictionary = [additionalParams mutableCopy];
        tempDictionary[@"subject_token_type"] = defaultJwtTokenType;
        additionalParams = [tempDictionary copy];
    }

    OIDAuthorizationRequest *request = [[OIDAuthorizationRequest alloc] initWithConfiguration:_configuration
                                                                                     clientId:_clientId
                                                                                       scopes:@[OIDScopeOpenID]
                                                                                  redirectURL:_callbackUrl
                                                                                 responseType:OIDResponseTypeCode
                                                                         additionalParameters:additionalParams];
    OIDTokenRequest *tokenRequest = [[OIDTokenRequest alloc] initWithConfiguration:_configuration
                                                                         grantType:defaultGrantType
                                                                 authorizationCode:nil
                                                                       redirectURL:_callbackUrl
                                                                          clientID:_clientId
                                                                      clientSecret:nil
                                                                             scope:@"openid"
                                                                      refreshToken:nil
                                                                      codeVerifier:nil
                                                              additionalParameters:additionalParams];
    OIDAuthState *auth;
    if ([[self instance] authState] != nil) {
        auth = [[self instance] authState];
    } else {
        auth = [OIDAuthState alloc];
    }
    
    UIViewController *controller = [self instance].GetRootViewController;
    [OIDAuthorizationService performTokenRequest:tokenRequest originalAuthorizationResponse:nil callback:^(OIDTokenResponse *_Nullable tokenResponse, NSError *_Nullable error) {
        if (error != nil) {
            NSLog(@"[TP AUTH] Token Exchange error: %@", [error localizedDescription]);
            [[self instance] UnityMessage:"TokenExchangeDidFail" stringMsg:[error localizedDescription]];
            return;
        }

        [auth updateWithTokenResponse:tokenResponse error:error];
        [[self instance] setAuthState:auth];
        [[self instance] saveState];
        [[self instance] UnityMessage:"TokenExchangeDidSucceed" stringMsg:[[self instance] GetStringifiedStateTokens]];
    }];
}

-  (NSDictionary *)GetStateTokens {
    NSDictionary *dict;
    if (_authState != nil) {
        OIDTokenResponse *response = _authState.lastTokenResponse;
        NSString *dateString = [self getUTCFormateDate:response.accessTokenExpirationDate];
        dict = @{
            @"AccessToken": response.accessToken,
            @"IdToken": response.idToken,
            @"RefreshToken": response.refreshToken,
            @"UTCExpirationDate": dateString
        };
    } else {
        dict = @{
            @"error": @"Tokens not found. Please authenticate the user."
        };
    }
    return dict;
}

-  (NSString *)getUTCFormateDate:(NSDate *)localDate {
    NSDateFormatter *dateFormatter = [[NSDateFormatter alloc] init];
    NSTimeZone *timeZone = [NSTimeZone timeZoneWithName:@"UTC"];
    [dateFormatter setTimeZone:timeZone];
    [dateFormatter setDateFormat:@"dd-MM-YYYY HH:mm:ss"];
    NSString *dateString = [dateFormatter stringFromDate:localDate];
    return dateString;
}

-  (NSString *)GetStringifiedStateTokens {
    NSDictionary *dict = [self GetStateTokens];
    return [self DictToJson:dict];
}

-  (NSString *)DictToJson:(NSDictionary *)dict {
    NSData *data = [NSJSONSerialization dataWithJSONObject:dict options:NSJSONWritingPrettyPrinted error:nil];
    NSString *jsonString = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
    return jsonString;
}

-  (UIViewController *)GetRootViewController {
    NSPredicate *filter = [NSPredicate predicateWithBlock:^BOOL(UIWindow *window, NSDictionary *bindings) {
        return [window isKeyWindow];
    }];
    
    NSArray *windows = [[UIApplication sharedApplication] windows];
    UIWindow *fistWindow = [[windows filteredArrayUsingPredicate:filter] firstObject];
    return fistWindow.rootViewController;
}

-  (void)UnityMessage:(char *)methodName {
    [self UnityMessage:methodName charMsg:""];
}

-  (void)UnityMessage:(char *)methodName charMsg:(const char *)param {
    UnitySendMessage(_TpMessageReceiver, methodName, param);
}

-  (void)UnityMessage:(char *)methodName stringMsg:(const NSString *_Nullable)param {
    if (param != nil) {
        [self UnityMessage:methodName charMsg:[param UTF8String]];
    } else {
        [self UnityMessage:methodName charMsg:""];
    }
}

-  (void)saveState {
    // TODO: STORE THIS IN KEYCHAIN
    NSUserDefaults *userDefaults = [[NSUserDefaults alloc] initWithSuiteName:suitName];
    NSData *archivedAuthState = [NSKeyedArchiver archivedDataWithRootObject:_authState];
    [userDefaults setObject:archivedAuthState forKey:kAuthStateKey];
    [userDefaults synchronize];
}

-  (void)loadState {
    NSUserDefaults* userDefaults = [[NSUserDefaults alloc] initWithSuiteName:suitName];
    NSData *archivedAuthState = [userDefaults objectForKey:kAuthStateKey];
    OIDAuthState *authState = [NSKeyedUnarchiver unarchiveObjectWithData:archivedAuthState];
    [self setAuthState:authState];
}

- (void)setAuthState:(nullable OIDAuthState *)authState {
    if (_authState == authState) {
        return;
    }
    _authState = authState;
    _authState.stateChangeDelegate = self;
    [self stateChanged];
}

- (void)stateChanged {
    [self saveState];
    //callbacks here
}

- (void)didChangeState:(nonnull OIDAuthState *)state {
    [self stateChanged];
}

@end

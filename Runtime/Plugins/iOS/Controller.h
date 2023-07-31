#import "AppAuth.h"
#ifndef Controller_h
#define Controller_h
@interface Controller : UIResponder <UIApplicationDelegate, OIDAuthStateChangeDelegate>
@property(nonatomic, strong, nullable) id<OIDExternalUserAgentSession> currentAuthorizationFlow;
@property(nonatomic, strong, nullable) OIDAuthState *authState;

+ (Controller * )instance;
+(void)Initialize:(char *) issuer: (char *) clientId: (char *) callbackUrl;
+(void)Authenticate;
+(void)GetTokens;
+(void)TokenExchange:(char *) token: (char *) issuer;


-(void) UnityMessage: ( char * _Nonnull) methodName charMsg:(const char *_Nullable) param;
-(void) UnityMessage: ( char * _Nonnull) methodName stringMsg:(NSString *_Nullable) param;
-(void) UnityMessage: ( char * _Nonnull) methodName;

@end
#endif

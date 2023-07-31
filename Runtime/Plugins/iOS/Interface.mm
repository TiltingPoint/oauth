#import "Controller.h"

extern "C" void InitializeInternal (char* issuer, char* clientId, char* callbackUrl){
    [Controller Initialize:issuer:clientId:callbackUrl];
}

extern "C" void AuthenticateInternal (){
    [Controller Authenticate];
}

//not used
extern "C" void TokenRequestInternal (){
    [Controller GetTokens];
}

//not used
extern "C" void TokenExchangeInternal (char* token, char* issuer){
    [Controller TokenExchange:token:issuer];
}

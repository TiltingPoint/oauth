package com.tiltingpoint.android;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.net.Uri;
import android.os.Bundle;
import android.util.Log;

import org.json.JSONException;
import org.json.JSONObject;

import net.openid.appauth.AuthState;
import net.openid.appauth.AuthorizationException;
import net.openid.appauth.AuthorizationRequest;
import net.openid.appauth.AuthorizationResponse;
import net.openid.appauth.AuthorizationService;
import net.openid.appauth.AuthorizationServiceConfiguration;
import net.openid.appauth.ResponseTypeValues;
import net.openid.appauth.TokenRequest;

import com.unity3d.player.UnityPlayer;

import java.text.DateFormat;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.HashMap;
import java.util.Map;

public class AuthActivity extends Activity {

    private static AuthorizationServiceConfiguration _authConfig;
    private static AuthState _authState;
    private static AuthorizationService _authService;
    private static Intent _authIntent;
    private static AuthActivity _authInstance;
    private static String _clientId;
    private static String _callbackUrl;
    private static Context _unityContext;
    private static int RC_AUTH = 100;
    private static final String TAG = "TiltingPointAuth";
    private static final String PREF_KEY = "TPAuth";
    private static final String TP_MESSAGE_RECEIVER = "TPAuth";
    private static final String EXCHANGE_GRANT_TYPE = "urn:ietf:params:oauth:grant-type:token-exchange";
    private static final String JWT_SUBJECT_TYPE = "urn:ietf:params:oauth:token-type:jwt";

    public static void initialize(String issuer, String clientId, String callbackUrl, Context context) {
        _clientId = clientId;
        _callbackUrl = callbackUrl;
        _unityContext = context;
        _authState = AuthActivity.getInstance().readAuthState();
        AuthorizationServiceConfiguration.fetchFromIssuer(Uri.parse(issuer), (serviceConfiguration, ex) -> {
            if (ex != null) {
                unityMessage("InitDidFail", ex.errorDescription);
                return;
            }
            _authConfig = serviceConfiguration;
            unityMessage("InitDidSucceed", "");
            if(_authState == null){
                AuthActivity.getInstance().writeAuthState(new AuthState(serviceConfiguration));
            }
        });
    }

    public static void authenticate() {
        /*
         * authenticate > onCreate > onActivityResult > internalAuthorization
         * authenticate starts the custom activity (needed to not to override Unity's activity)
         * onCreate gets called when the activity is ready and triggers auth activity waiting for the auth result
         * internalAuthorization does the token exchange.
         */
        unityMessage("AuthWillStart", "");

        AuthorizationRequest.Builder authRequestBuilder =
                new AuthorizationRequest.Builder(
                    _authConfig, // the authorization service configuration
                    _clientId, // the client ID, typically pre-registered and static
                    ResponseTypeValues.CODE, // the response_type value: we want a code,
                    Uri.parse(_callbackUrl)
                );
        authRequestBuilder.setScope("openid");

        //New activity needs to be launched from Unity's activity so we don't have to override it.
        Intent tempIntent = new Intent(UnityPlayer.currentActivity, AuthActivity.class);
        UnityPlayer.currentActivity.startActivity(tempIntent);

        _authService = new AuthorizationService(_unityContext);
        Intent authIntent = _authService.getAuthorizationRequestIntent(authRequestBuilder.build());

        _authIntent = authIntent;
    }

    public static void tokenExchange(String token, String issuer){
        _authService = new AuthorizationService(_unityContext);
        //Create the request
        TokenRequest.Builder builder = new TokenRequest.Builder(_authConfig, _clientId);
        builder.setScope("openid");
        builder.setGrantType(EXCHANGE_GRANT_TYPE);
        Map<String, String> additionalExchangeParameters = new HashMap<>();
        additionalExchangeParameters.put("subject_issuer", issuer);
        additionalExchangeParameters.put("subject_token", token);

        builder.setAdditionalParameters(additionalExchangeParameters);
        TokenRequest authRequest = builder.build();

        _authService.performTokenRequest(authRequest, (resp, ex) -> {
            _authState.update(resp, ex);
            AuthActivity.getInstance().writeAuthState(_authState);
            if (resp != null) {
                //TODO: unify this in one method shared by auth and exchange.
                JSONObject json = new JSONObject();
                Date currentDate = new Date(resp.accessTokenExpirationTime);
                DateFormat df = new SimpleDateFormat("dd-MM-YYYY HH:mm:ss");
                try {
                    json.put("AccessToken", resp.accessToken);
                    json.put("IdToken", resp.idToken);
                    json.put("RefreshToken", resp.refreshToken);
                    json.put("UTCExpirationDate", df.format(currentDate));
                } catch (JSONException e) {
                    unityMessage("TokenExchangeDidFail", "Error forming token response.");
                    return;
                }
                unityMessage("TokenExchangeDidSucceed", json.toString());
            } else {
                unityMessage("TokenExchangeDidFail", ex.errorDescription);
            }
        });
    }

    public static void getTokens() {
        if(_authState != null) {
            if(_authService == null){
                _authService = new AuthorizationService(_unityContext);
            }
            _authState.performActionWithFreshTokens(_authService, new AuthState.AuthStateAction() {
                @Override public void execute(
                    String accessToken,
                    String idToken,
                    AuthorizationException ex) {
                    if (ex != null) {
                        unityMessage("TokenRequestDidFail", "Please authenticate your user first. " + ex.toString());
                        return;
                    }
                    JSONObject json = new JSONObject();
                    try {
                        json.put("AccessToken", accessToken);
                        json.put("IdToken", idToken);
                        json.put("RefreshToken", _authState.getRefreshToken());
                    } catch (JSONException e) {
                        unityMessage("TokenRequestDidFail", "Error forming token response.");
                        return;
                    }
                    unityMessage("TokenRequestDidSucceed", json.toString());
                }
            });
        } else {
            unityMessage("TokenRequestDidFail", "Please authenticate your user first.");
        }
    }

    private static AuthActivity getInstance(){
        if(_authInstance == null)
            _authInstance = new AuthActivity();
        return _authInstance;
    }

    private static void unityMessage(String method, String msg) {
        UnityPlayer.UnitySendMessage(TP_MESSAGE_RECEIVER, method, msg);
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        _authInstance = this;
        startActivityForResult(_authIntent, RC_AUTH);
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        if (requestCode == RC_AUTH) {
            AuthorizationResponse resp = AuthorizationResponse.fromIntent(data);
            AuthorizationException ex = AuthorizationException.fromIntent(data);
            if(ex == null) {
                internalAuthorization(resp);
            }
            else {
                unityMessage("AuthDidFail", ex.errorDescription);
                finish();
            }
        } else {
            unityMessage("AuthDidFail", "Activity result code did not match.");
            finish();
        }
    }

    private void internalAuthorization(AuthorizationResponse authResponse) {
        _authService.performTokenRequest(authResponse.createTokenExchangeRequest(), (resp, ex) -> {
            _authState.update(resp, ex);
            writeAuthState(_authState);
            if (resp != null) {
                JSONObject json = new JSONObject();
                Date currentDate = new Date(resp.accessTokenExpirationTime);
                DateFormat df = new SimpleDateFormat("dd-MM-YYYY HH:mm:ss");
                try {
                    json.put("AccessToken", resp.accessToken);
                    json.put("IdToken", resp.idToken);
                    json.put("RefreshToken", resp.refreshToken);
                    json.put("UTCExpirationDate", df.format(currentDate).toString());
                } catch (JSONException e) {
                    unityMessage("AuthDidFail", "Error forming token response.");
                    return;
                }
                unityMessage("AuthDidSucceed", json.toString());
            } else {
                unityMessage("AuthDidFail", ex.errorDescription);
            }
            finish();
        });
    }

    private AuthState readAuthState() {
        SharedPreferences authPrefs = _unityContext.getSharedPreferences(PREF_KEY, MODE_PRIVATE);
        String stateJson = authPrefs.getString("stateJson", null);
        if (stateJson != null) {
            try {
                return AuthState.jsonDeserialize(stateJson);
            } catch (JSONException e) {
                e.printStackTrace();
                return null;
            }
        }
        return null;
    }

    private void writeAuthState(AuthState state) {
        _authState = state;
        SharedPreferences authPrefs = _unityContext.getSharedPreferences(PREF_KEY, MODE_PRIVATE);
        authPrefs.edit()
                .putString("stateJson", state.jsonSerializeString())
                .apply();
    }
}
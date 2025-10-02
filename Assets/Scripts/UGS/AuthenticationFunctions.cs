using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // <-- Make sure you include this
using Unity.Services.Core;
using Unity.Services.Authentication;
using System;
using System.Threading.Tasks;
using TMPro;

public class AuthenticationFunctions : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI debugText;

    [SerializeField] private InputField signInUsernameInput;
    [SerializeField] private InputField signInPasswordInput;
    [SerializeField] private InputField signUpUsernameInput;
    [SerializeField] private InputField signUpPasswordInput;

    // The name (or index) of the scene you want to move to after successful sign-in
    [SerializeField] private string MAIN_SCENE_NAME = "MainScene";
    // The name (or index) of the login scene (this scene)
    [SerializeField] private string LOGIN_SCENE_NAME = "LoginScene";

    /// <summary>
    /// Initializes Unity Services (call once, e.g., on a "Start" button or in Start()).
    /// </summary>
    public async void InitializeUGS()
    {
        if (UnityServices.State == ServicesInitializationState.Initialized)
        {
            UpdateDebugText("UGS already initialized.");
            return;
        }

        try
        {
            UpdateDebugText("Initializing Unity Services...");
            await UnityServices.InitializeAsync();
            UpdateDebugText("UGS initialized successfully.");
        }
        catch (Exception e)
        {
            UpdateDebugText($"UGS initialization failed: {e.Message}");
        }
    }

    /// <summary>
    /// Signs in anonymously with Unity Authentication.
    /// After success, load the main scene.
    /// </summary>
    public async void SignInAnonymously()
    {
        if (!await EnsureServicesInitialized()) return;

        UpdateDebugText("Attempting anonymous sign-in...");

        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            UpdateDebugText($"Anonymous sign-in successful!\nPlayer ID: {AuthenticationService.Instance.PlayerId}");

            // Move to next scene
            SceneManager.LoadScene(MAIN_SCENE_NAME);
        }
        catch (Exception e)
        {
            UpdateDebugText($"Anonymous sign-in failed: {e.Message}");
        }
    }

    /// <summary>
    /// "Sign Up" with a username & password (using custom/experimental methods).
    /// After success, remain in the login scene (or go to main—your choice).
    /// </summary>
    public async void SignUpWithUsernamePassword()
    {
        if (!await EnsureServicesInitialized()) return;

        string username = signUpUsernameInput?.text;
        string password = signUpPasswordInput?.text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            UpdateDebugText("Sign Up failed: Username or password is empty.");
            return;
        }

        UpdateDebugText($"Attempting to Sign Up as '{username}'...");

        try
        {
            await SignOutIfSignedInAsync();

            // Custom method in your project
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);

            UpdateDebugText($"Sign Up successful!\nPlayer ID: {AuthenticationService.Instance.PlayerId}");

            // Optionally, you could auto-sign in or stay here, depending on your design
        }
        catch (Exception e)
        {
            UpdateDebugText($"Sign Up failed: {e.Message}");
        }
    }

    /// <summary>
    /// "Sign In" with a username & password.
    /// After success, move to the main scene.
    /// </summary>
    public async void SignInWithUsernamePassword()
    {
        if (!await EnsureServicesInitialized()) return;

        string username = signInUsernameInput?.text;
        string password = signInPasswordInput?.text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            UpdateDebugText("Sign In failed: Username or password is empty.");
            return;
        }

        UpdateDebugText($"Attempting to Sign In as '{username}'...");

        try
        {
            await SignOutIfSignedInAsync();

            // Custom method in your project
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);

            UpdateDebugText($"Sign In successful!\nPlayer ID: {AuthenticationService.Instance.PlayerId}");

            // Move to next scene after successful sign-in
            SceneManager.LoadScene(MAIN_SCENE_NAME);
        }
        catch (Exception e)
        {
            UpdateDebugText($"Sign In failed: {e.Message}");
        }
    }

    /// <summary>
    /// Public method to sign out explicitly (assign this to a "Sign Out" button).
    /// After sign out, go back to the login scene.
    /// </summary>
    public void SignOut()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            UpdateDebugText("No user is currently signed in.");
            return;
        }

        try
        {
            // Using synchronous SignOut
            AuthenticationService.Instance.SignOut();
            UpdateDebugText("Successfully signed out.");

            // Move back to the login scene
            SceneManager.LoadScene(LOGIN_SCENE_NAME);
        }
        catch (Exception e)
        {
            UpdateDebugText($"Sign Out failed: {e.Message}");
        }
    }

    /// <summary>
    /// Signs out the current user if they are signed in (used internally before switching user).
    /// </summary>
    private async Task SignOutIfSignedInAsync()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            try
            {
                // Using synchronous SignOut
                AuthenticationService.Instance.SignOut();
            }
            catch (Exception e)
            {
                UpdateDebugText($"Sign Out failed: {e.Message}");
            }
        }

        // Return an immediately completed Task so that this remains an awaitable method
        await Task.CompletedTask;
    }

    /// <summary>
    /// Checks if Unity Services are initialized; if not, attempts to initialize them.
    /// Returns true if services are ready, false otherwise.
    /// </summary>
    private async Task<bool> EnsureServicesInitialized()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            try
            {
                UpdateDebugText("Unity Services not initialized. Initializing now...");
                await UnityServices.InitializeAsync();
                UpdateDebugText("UGS initialized successfully.");
            }
            catch (Exception e)
            {
                UpdateDebugText($"UGS initialization failed: {e.Message}");
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Helper method to update the debug text (if assigned).
    /// Falls back to Debug.Log if no UI element is assigned.
    /// </summary>
    private void UpdateDebugText(string message)
    {
        if (debugText != null)
        {
            debugText.text = message;
        }
        else
        {
            Debug.Log(message);
        }
    }
}

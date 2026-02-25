using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using GorillaFM.Patches;
using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Objects;
using UnityEngine;
using UnityEngine.Networking;

namespace GorillaFM;

[BepInPlugin(Constants.Guid, Constants.Name, Constants.Version)]
public class Main : BaseUnityPlugin
{
    internal const string PlaceHolder = "_______";
    public static Main? Instance;

    internal ConfigEntry<string>? APIKey, APISecret, Username, Password;
    internal ConfigEntry<bool>? ShowExampleUI;

    public GorillaFm? gorillaFm;

    public Main()
    {
        Instance = this;
        //HarmonyPatches.Patch();
    }

    private void Start()
    {
        APIKey = Config.Bind("KEEP HIDDEN FROM OTHERS", "APIKey", PlaceHolder);
        APISecret = Config.Bind("KEEP HIDDEN FROM OTHERS", "APISecret", PlaceHolder);
        Username = Config.Bind("KEEP HIDDEN FROM OTHERS", "Username", PlaceHolder);
        Password = Config.Bind("KEEP HIDDEN FROM OTHERS", "Password", PlaceHolder);
        ShowExampleUI = Config.Bind("Settings",  "Show Example UI", false);
        GorillaTagger.OnPlayerSpawned(OnPlayerSpawn);
    }

    private void OnPlayerSpawn()
    {
        gorillaFm = new GameObject("Gorilla FM").AddComponent<GorillaFm>();
        DontDestroyOnLoad(gorillaFm.gameObject);
    }
}

public class GorillaFm : MonoBehaviour
{
    private static LastfmClient? _lastfmClient;

    private static GUISkin? _guiSkin;

    public static LastTrack? CurrentTrack;
    
    public static Action? SongChangedCallback;

    private string? _cachedTrackName = "";
    public Texture2D? trackTexture;

    private readonly CancellationTokenSource _cts = new();
    
    public void OnSongChanged(Action action)
    { 
        SongChangedCallback += action;
    }

    private void Start()
    {
        try
        {
            if (Main.Instance?.APIKey!.Value == Main.PlaceHolder &&
                Main.Instance.APISecret!.Value == Main.PlaceHolder &&
                Main.Instance.Username!.Value == Main.PlaceHolder &&
                Main.Instance.Password!.Value == Main.PlaceHolder)
                return;

            _guiSkin = Resources.FindObjectsOfTypeAll<GUISkin>()
                .FirstOrDefault(s => s.name == "Drone GUISkin");

            _lastfmClient = new LastfmClient(Main.Instance?.APIKey!.Value, Main.Instance?.APISecret!.Value);

            RepeatGetCurrentSong(_cts.Token);
            
            Main.Instance?.Logger.LogInfo($"[{Constants.Name}] I have loaded!");
        }
        catch (Exception e)
        {
            Main.Instance?.Logger.LogError($"[{Constants.Name}] Player Start Failed:{e.Message + e.StackTrace}");
        }
    }
    
    private const int FontSize = 32;
    private void OnGUI()
    {
        if (CurrentTrack == null || !Main.Instance!.ShowExampleUI!.Value)
            return;

        GUILayout.BeginVertical(_guiSkin?.box ?? GUI.skin.box);
        if (trackTexture)
        {
            const float imageWidth = 256f;
            const float imageHeight = 256f;

            var width = imageWidth;
            var height = imageHeight;

            var aspectRatio = (float)trackTexture.width / trackTexture.height;
            if (aspectRatio > 1f)
                height = imageWidth / aspectRatio;
            else
                width = imageHeight * aspectRatio;

            GUILayout.Label(trackTexture, GUILayout.Width(width), GUILayout.Height(height));
        }
        
        var labelStyle = new GUIStyle(_guiSkin?.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = FontSize,
            wordWrap = false
        };
        
        GUILayout.Label(CurrentTrack.Name.ToUpper(), labelStyle);
        GUILayout.Label(CurrentTrack.ArtistName.ToUpper(), labelStyle);
        GUILayout.Label(CurrentTrack.AlbumName.ToUpper(), labelStyle);
        GUILayout.Label($"Playing Now?: {CurrentTrack.IsNowPlaying}".ToUpper(), labelStyle);

        GUILayout.EndVertical();
    }


    private async void RepeatGetCurrentSong(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Run(async () =>
                {
                    var recentTracks =
                        await _lastfmClient?.User.GetRecentScrobbles(Main.Instance?.Username!.Value, count: 1)!;
                    CurrentTrack = recentTracks.Content.FirstOrDefault();
                    if (_cachedTrackName != CurrentTrack?.Name)
                    {
                        _cachedTrackName = CurrentTrack?.Name;

                        if (CurrentTrack?.Images?.ExtraLarge != null)
                        {
                            StartCoroutine(LoadImage(CurrentTrack.Images.ExtraLarge.AbsoluteUri));
                        }
                    }
                }, token);

                await Task.Delay(5000, token);
            }
        }
        catch (Exception e)
        {
            Main.Instance?.Logger.LogError(
                $"[{Constants.Name}] Getting recent Track Failed:{e.Message + e.StackTrace}");
            _cts.Cancel();
        }
    }
    
    private IEnumerator LoadImage(string imageUrl)
    {
        using var request = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Main.Instance?.Logger.LogError($"[{Constants.Name}] Failed to load image: {request.error}");
        }
        else
        {
            trackTexture = DownloadHandlerTexture.GetContent(request);
        }
        var callback = SongChangedCallback;
        callback();
    }
    
    
}
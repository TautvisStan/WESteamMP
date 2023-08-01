using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using UnityEngine;
using System.Collections;
using Steamworks;
using Mirror;
using Mirror.FizzySteam;


namespace SteamMultiplayer
{
    [BepInPlugin(PluginGuid, PluginName, PluginVer)]
    [HarmonyPatch]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "GeeEm.WrestlingEmpire.SteamMultiplayer";
        public const string PluginName = "SteamMultiplayer";
        public const string PluginVer = "0.0.1";

        internal static ManualLogSource Log;
        internal readonly static Harmony Harmony = new(PluginGuid);

        internal static string PluginPath;
        public GameObject SteamStuff;
        public NetworkManager manager;
        public FizzySteamworks steamworks;
        public SteamManager steammanager;

        private void Awake()
        {
            Plugin.Log = base.Logger;

        }

        private void OnEnable()
        {
            Harmony.PatchAll();
            Logger.LogInfo($"Loaded {PluginName}!");
            PluginPath = Path.GetDirectoryName(Info.Location);
            SteamStuff = Instantiate(new GameObject("SteamStuff"));

            steammanager = SteamStuff.AddComponent<SteamManager>();
              steamworks = SteamStuff.AddComponent<FizzySteamworks>();
              manager = SteamStuff.AddComponent<NetworkManager>();
                 manager.autoCreatePlayer = false;

                 manager.transport = steamworks;


            SteamStuff.AddComponent<SteamLobby>();
            GameObject.DontDestroyOnLoad(SteamStuff);
        }

        private void OnDisable()
        {
            Harmony.UnpatchSelf();
            Logger.LogInfo($"Unloaded {PluginName}!");
        }
        public class SteamLobby : MonoBehaviour
        {
            protected Callback<LobbyCreated_t> LobbyCreated;
            protected Callback<GameLobbyJoinRequested_t> JoinRequest;
            protected Callback<LobbyEnter_t> LobbyEntered;
            public ulong CurrentLobbyID;
            private const string HostAddressKey = "HostAddress";
           private NetworkManager manager;

            void Start()
            {

                
                if(!SteamManager.LKNMNDFOHGD) { Plugin.Log.LogWarning("steam stuff off"); return; }
                Plugin.Log.LogWarning("steam stuff on");
                manager = GetComponent<NetworkManager>();
                LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
                JoinRequest = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
                LobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
                
            }
            void Update()
            {
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    HostLobby();
                }
            }
            public void HostLobby()
            {
                SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, manager.maxConnections);
            }
            private void OnLobbyCreated(LobbyCreated_t callback)
            {
                if(callback.m_eResult != EResult.k_EResultOK) { return; }
                manager.StartHost();
                SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey, SteamUser.GetSteamID().ToString());
                SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), "name", SteamFriends.GetPersonaName().ToString());
               
               
                Plugin.Log.LogWarning("Lobby created");

            }
            private void OnJoinRequest(GameLobbyJoinRequested_t callback)
            {
                Plugin.Log.LogWarning("Requested to join");
                SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
            }
            private void OnLobbyEntered(LobbyEnter_t callback)
            {
                //everyone
                Plugin.Log.LogWarning("Lobby entered");
                
                CurrentLobbyID = callback.m_ulSteamIDLobby;

                //clients
                if(NetworkServer.active) { return; }
                manager.networkAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey);
                manager.StartClient();
                
            }
        }
    }
}
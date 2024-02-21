using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace Hashbyte.Multiplayer
{
    public class UnityGameRoomService : LobbyEventCallbacks, IGameRoomService, IRelayService, ILobbyService
    {
        private UnityRoomResponse roomResponse;
        private List<INetworkEvents> networkEventListeners;        

        public UnityGameRoomService()
        {
            PlayerJoined += OnPlayerJoined;
            DataChanged += LobbyDataChanged;

        }
        private void LobbyDataChanged(Dictionary<string, ChangedOrRemovedLobbyValue<DataObject>> obj)
        {
            if (obj != null && obj.Count > 0)
            {
                Hashtable roomProperties = new Hashtable(); 
                foreach (string key in obj.Keys)
                {
                    roomProperties.Add(key, obj[key].Value);  
                    Debug.Log($"Data added {key} -- {obj[key].Value}");
                }
                foreach (INetworkEvents netEventListener in networkEventListeners)
                    netEventListener.OnRoomPropertiesUpdated(roomProperties);
            }
        }

        private void OnPlayerJoined(List<LobbyPlayerJoined> playersJoined)
        {
            foreach (INetworkEvents netEventListener in networkEventListeners)
                foreach (LobbyPlayerJoined lobbyPlayer in playersJoined)
                    netEventListener.OnPlayerJoined(/*lobbyPlayer.PlayerIndex, lobbyPlayer.Player.Id, ""*/);
        }

        public void RegisterCallbacks(INetworkEvents networkEvents)
        {
            if (networkEventListeners == null) networkEventListeners = new List<INetworkEvents>();
            networkEventListeners.Add(networkEvents);
        }

        public async Task<IRoomResponse> CreateRoom(bool isPrivate)
        {
            string sessionId = await CreateRelaySession(Constants.kRegionForServer);
            await CreateLobby(sessionId, Constants.kMaxPlayers, sessionId);
            roomResponse.RoomId = sessionId;
            roomResponse.Success = true;
            return roomResponse;
        }

        public async Task<IRoomResponse> JoinRandomRoom()
        {
            Debug.Log($"Joining random room ");
            try
            {
                roomResponse = new UnityRoomResponse();
                string roomId = await JoinLobby("", "");
                if (string.IsNullOrEmpty(roomId))
                {
                    return await CreateRoom(false);
                }
                else
                {
                    await JoinRelaySession(roomId);
                    return roomResponse;
                }
            }
            catch (RelayServiceException exception)
            {
                roomResponse = new UnityRoomResponse();
                roomResponse.Success = false;
                roomResponse.Error = new RoomError()
                {
                    ErrorCode = exception.ErrorCode,
                    Message = exception.Message,
                };
                return roomResponse;
            }
        }
        public async Task<string> CreateRelaySession(string region)
        {
            Allocation relayAllocation = await RelayService.Instance.CreateAllocationAsync(Constants.kMaxPlayers, region);
            roomResponse.isHost = true;
            roomResponse.hostAllocation = relayAllocation;
            return await RelayService.Instance.GetJoinCodeAsync(relayAllocation.AllocationId);
        }
        public async Task JoinRelaySession(string sessionId)
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(sessionId);
            roomResponse.isHost = false;
            roomResponse.clientAllocation = allocation;
            roomResponse.Success = true;
            roomResponse.RoomId = sessionId;
        }

        public async Task CreateLobby(string lobbyName, int maxPlayers, object additionalData)
        {
            CreateLobbyOptions options = new CreateLobbyOptions()
            {
                Data = new Dictionary<string, DataObject> { { Constants.kRoomId, new DataObject(DataObject.VisibilityOptions.Public, additionalData.ToString()) } }
            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 2, options);
            try
            {
                await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, this);
            }
            catch (LobbyServiceException ex)
            {
                Debug.Log(ex.Reason.ToString());
            }
            roomResponse.LobbyId = lobby.Id;
        }

        public async Task<string> JoinLobby(string lobbyId, object additionalData)
        {
            try
            {
                Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync();
                roomResponse.LobbyId = lobbyId;
                try
                {
                    await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, this);
                }
                catch (LobbyServiceException ex)
                {
                    Debug.Log(ex.Reason.ToString());
                }
                return lobby == null ? "" : lobby.Data[Constants.kRoomId].Value;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e.Reason.ToString());
                return null;
            }
        }

        public async Task UpdateLobbyData(string lobbyId, Hashtable dataToUpdate)
        {
            UpdateLobbyOptions options = new UpdateLobbyOptions();
            options.Data = new Dictionary<string, DataObject>();
            foreach (var dataObj in dataToUpdate.Keys)
            {
                options.Data.Add(dataObj.ToString(), new DataObject(visibility: DataObject.VisibilityOptions.Public, value: dataToUpdate[dataObj].ToString()));
            }
            Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(lobbyId, options);
            Debug.Log($"Room property updated {lobby.Data["seed"].Value}");
        }

        public async Task UpdateRoomProperties(string roomID, Hashtable roomProperties)
        {
            await UpdateLobbyData(roomID, roomProperties);
        }
    }
}

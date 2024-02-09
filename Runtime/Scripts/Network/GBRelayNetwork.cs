using Unity.Networking.Transport;
using Unity.Services.Relay.Models;
using Unity.Collections;
namespace Hashbyte.Multiplayer
{
    public abstract class GBRelayNetwork
    {

        protected IRelayEvents relayEventListener;
        protected Allocation gameSession;

        protected NetworkDriver driver;  
        public GBRelayNetwork(IRelayEvents relayEventListener)
        {
            this.relayEventListener = relayEventListener;            
        }

        public virtual void NetworkUpdate()
        {
            if (!driver.IsCreated || !driver.Bound)
            {
                return;
            }
            //Keep relay server alive
            driver.ScheduleUpdate().Complete();
        }

        public abstract void SendEvent(GameEvent gameEvent);

        public virtual void ReceiveEvent(FixedString32Bytes eventData)
        {
            GameEvent gameEvent = new GameEvent();
            string[] eventSplit = eventData.ToString().Split(':');
            if(eventSplit.Length > 0)
            {
                int evType = int.Parse(eventSplit[0]);
                gameEvent.eventType = (GameEventType)evType;
                if(eventSplit.Length > 1)
                {
                    gameEvent.data = eventSplit[1];
                }
            }
            relayEventListener?.OnEvent(gameEvent);
        }

        protected virtual void SendGameEvent(NetworkConnection connection, GameEvent gameEvent)
        {
            Debug.Log($"Base Send Event {gameEvent.eventType}");
            if (driver.BeginSend(connection, out var writer) == 0)
            {
                FixedString32Bytes msg = $"{(int)gameEvent.eventType}:{gameEvent.data}";
                // Send the message. Aside from FixedString32, many different types can be used.
                writer.WriteFixedString32(msg);
                Debug.Log($"Base Event Msg {msg}");
                driver.EndSend(writer);
            }
        }

        public virtual void OnDestroy()
        {
            driver.Dispose();
        }

    }
}

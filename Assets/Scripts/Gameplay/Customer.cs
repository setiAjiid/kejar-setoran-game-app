using UnityEngine;
using KejarSetoran.Graph;
using KejarSetoran.Visual;

namespace KejarSetoran.Gameplay
{
    public enum CustomerState { Waiting, OnBoard, Delivered, TimedOut }

    public class Customer : MonoBehaviour
    {
        public MapNode pickupNode;
        public MapNode destinationNode;
        public CustomerState state = CustomerState.Waiting;

        public float pickupTimeoutSeconds = 30f;
        public float deliveryTimeBudget;
        public float spawnTime;
        public int fareDistance; // Dijkstra cost from pickup → destination, locked at booking

        private GameObject pickupMarker;
        private GameObject destMarker;

        public void Init(MapNode pickup, MapNode destination, float deliveryBudget)
        {
            pickupNode = pickup;
            destinationNode = destination;
            deliveryTimeBudget = deliveryBudget;
            spawnTime = Time.time;
            state = CustomerState.Waiting;

            pickupMarker = SpriteFactory.CreateCustomerMarker(transform, new Color(0.95f, 0.4f, 0.4f, 1f));
            pickupMarker.transform.position = pickup.transform.position + new Vector3(0f, 0.55f, 0f);
            pickupMarker.GetComponent<SpriteRenderer>().color = new Color(0.95f, 0.4f, 0.4f, 1f);

            destMarker = SpriteFactory.CreateCustomerMarker(transform, new Color(0.3f, 0.95f, 0.4f, 1f));
            destMarker.transform.position = destination.transform.position + new Vector3(0f, 0.55f, 0f);
            destMarker.GetComponent<SpriteRenderer>().color = new Color(0.3f, 0.95f, 0.4f, 1f);
            destMarker.SetActive(false); // shown after pickup
        }

        public float PickupTimeLeft()
        {
            return Mathf.Max(0f, pickupTimeoutSeconds - (Time.time - spawnTime));
        }

        public void MarkOnBoard()
        {
            state = CustomerState.OnBoard;
            if (pickupMarker != null) pickupMarker.SetActive(false);
            if (destMarker != null) destMarker.SetActive(true);
        }

        public void MarkDelivered()
        {
            state = CustomerState.Delivered;
            if (pickupMarker != null) Destroy(pickupMarker);
            if (destMarker != null) Destroy(destMarker);
        }

        public void MarkTimedOut()
        {
            state = CustomerState.TimedOut;
        }

        private void Update()
        {
            if (state == CustomerState.Waiting && pickupMarker != null)
            {
                float t = Mathf.Sin(Time.time * 5f) * 0.5f + 0.5f;
                var sr = pickupMarker.GetComponent<SpriteRenderer>();
                sr.color = Color.Lerp(new Color(0.95f, 0.4f, 0.4f, 1f), new Color(1f, 0.85f, 0.2f, 1f), t);
            }
        }
    }
}

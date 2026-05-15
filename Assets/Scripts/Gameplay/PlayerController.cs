using System;
using UnityEngine;
using KejarSetoran.Graph;
using KejarSetoran.Managers;
using KejarSetoran.Visual;

namespace KejarSetoran.Gameplay
{
    public class PlayerController : MonoBehaviour
    {
        public MapNode CurrentNode { get; private set; }
        public bool IsMoving { get; private set; }

        public float moveSpeed = 4.5f;
        public event Action<MapNode> OnArriveAtNode;
        public event Action<MapNode, MapNode> OnDepartTowards;

        private MapNode targetNode;
        private Vector3 moveStart;
        private float moveProgress;
        private float moveDuration;
        private GameManager game;

        // Sprite stack offset: angle (degrees) to add when the source art's
        // "natural" facing isn't East. If the bike art faces north at
        // rotation 0, set this to -90. Tweak after first playtest.
        private const float SpriteFacingOffset = 0f;

        private Transform[] sliceTransforms;
        private float currentFacingAngle = float.NaN;

        public void Init(MapNode startNode, GameManager game)
        {
            this.game = game;
            CurrentNode = startNode;
            transform.position = startNode.transform.position;

            // Cache only direct slice children (skip the player root itself).
            var srs = GetComponentsInChildren<SpriteRenderer>();
            sliceTransforms = new Transform[srs.Length];
            for (int i = 0; i < srs.Length; i++) sliceTransforms[i] = srs[i].transform;
        }

        private void SetFacing(Vector2 dir)
        {
            if (sliceTransforms == null || sliceTransforms.Length == 0) return;
            if (dir.sqrMagnitude < 0.0001f) return;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + SpriteFacingOffset;
            if (Mathf.Approximately(angle, currentFacingAngle)) return;
            currentFacingAngle = angle;
            var rot = Quaternion.Euler(0f, 0f, angle);
            for (int i = 0; i < sliceTransforms.Length; i++)
            {
                sliceTransforms[i].localRotation = rot;
            }
        }

        private void Update()
        {
            if (game == null || game.State != GameState.Playing) return;

            if (IsMoving)
            {
                moveProgress += Time.deltaTime / moveDuration;
                transform.position = Vector3.Lerp(moveStart, targetNode.transform.position, Mathf.SmoothStep(0, 1, Mathf.Clamp01(moveProgress)));
                if (moveProgress >= 1f)
                {
                    transform.position = targetNode.transform.position;
                    CurrentNode = targetNode;
                    targetNode = null;
                    IsMoving = false;
                    OnArriveAtNode?.Invoke(CurrentNode);
                }
                return;
            }

            Vector2 dir = Vector2.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) dir = Vector2.up;
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) dir = Vector2.down;
            else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dir = Vector2.left;
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dir = Vector2.right;

            if (dir != Vector2.zero)
            {
                var next = GraphManager.Instance.NearestNeighborInDirection(CurrentNode, dir);
                if (next != null) BeginMoveTo(next);
            }
        }

        private void BeginMoveTo(MapNode node)
        {
            OnDepartTowards?.Invoke(CurrentNode, node);
            targetNode = node;
            moveStart = transform.position;
            moveProgress = 0f;
            float dist = Vector3.Distance(moveStart, node.transform.position);
            moveDuration = Mathf.Max(0.15f, dist / moveSpeed);
            IsMoving = true;
            SetFacing(node.transform.position - moveStart);
        }
    }
}

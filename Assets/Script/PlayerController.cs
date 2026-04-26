using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.Rendering.Universal;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;

namespace Cainos.PixelArtTopDown_Basic
{
    public class PlayerController : MonoBehaviourPunCallbacks
    {
        [Header("Movement")]
        public float walkSpeed = 5f;
        public float sprintSpeed = 8f;

        [Header("Mana")]
        public float maxMana = 100f;
        public float currentMana = 100f;
        public float manaDrainRate = 5f; // Mana drained per second

        [Header("Visual")]
        public SpriteRenderer spriteRenderer;
        public TextMeshProUGUI playerNameText;
        public Image manaBarFill;
        public Color humanColor = Color.white;
        public Color ghostColor = Color.red;
        public Color eliminatedColor = new Color(1f, 1f, 1f, 0.2f); // Trắng mờ (alpha = 0.2)

        [Header("Lighting")]
        private Light2D playerLight;
        public float playerLightRadius = 5f;

        private Animator animator;
        private Rigidbody2D rb;
        private PhotonView pv;

        private bool isGhost = false;
        private bool isEliminated = false; // Người bị bắt (loại bỏ)

        public bool IsGhost { get { return isGhost; } }
        public bool IsEliminated { get { return isEliminated; } }

        void Awake()
        {
            animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();
            pv = GetComponent<PhotonView>();
        }

        private void Start()
        {
            // Setup player light (spotlight around player)
            if (pv.IsMine)
            {
                playerNameText.text = PhotonNetwork.NickName;
                SetupPlayerLight();
            }
            else
            {
                // Disable light for other players
                if (playerLight != null)
                    playerLight.enabled = false;
                playerNameText.text = pv.Owner.NickName; // Hiển thị tên người chơi khác
            }

            // Register with game manager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterPlayer(this);
            }

            // Set initial color
            UpdateVisuals();
        }
        private void OnDestroy() 
        {
            // Unregister from game manager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UnregisterPlayer(this);
            }
        }

        private void SetupPlayerLight()
        {
            // Create light if not assigned
            GameObject lightObj = new GameObject("PlayerLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.zero;

            playerLight = lightObj.AddComponent<Light2D>();

            playerLight.lightType = Light2D.LightType.Point;
            playerLight.intensity = 1f;
            playerLight.pointLightOuterRadius = playerLightRadius;
            playerLight.color = Color.white;
        }

        private void Update()
        {
            if (!pv.IsMine) return;

            // Không cho di chuyển nếu bị loại
            if (isEliminated) return;

            HandleMovement();
        }

        private void HandleMovement()
        {
            Vector2 dir = Vector2.zero;

            dir.x = Input.GetAxisRaw("Horizontal");
            dir.y = Input.GetAxisRaw("Vertical");
            if (dir.x != 0 || dir.y != 0)
            {
                animator.SetFloat("X", dir.x);
                animator.SetFloat("Y", dir.y);
            }
            animator.SetFloat("Speed", dir.magnitude);

            dir.Normalize();
            bool isMoving = dir.magnitude > 0;
            bool isSprinting = Input.GetKey(KeyCode.LeftShift) && currentMana > 0 && isMoving;

            float currentSpeed = walkSpeed;

            if (isSprinting)
            {
                currentSpeed = sprintSpeed;
                currentMana -= manaDrainRate * Time.deltaTime;
            }

            currentMana = Mathf.Clamp(currentMana, 0, maxMana);

            rb.linearVelocity = currentSpeed * dir;
            if(Input.GetKeyDown(KeyCode.Space))
            {
                PlayerNotification notification = GetComponent<PlayerNotification>();
                if(notification != null)
                {
                    notification.ShowNotification("Hello, this is a test notification!");
                }
                else
                {
                    Debug.LogWarning("PlayerNotification component not found on player!");
                }
            }
            UpdateUIMana();
        }

        void UpdateUIMana()
        {
            if (manaBarFill != null)
            {
                manaBarFill.fillAmount = currentMana / maxMana;
            }
        }

        [PunRPC]
        public void SetAsGhost()
        {
            isGhost = true;
            isEliminated = false; // Ma không bị loại
            UpdateVisuals();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdatePlayerCounts();
            }

            // Show notification if this is local player
            if (pv.IsMine)
            {
                Debug.Log("You are now a GHOST! Tag all other players!");
            }
        }

        [PunRPC]
        public void SetAsEliminated()
        {
            isGhost = false;
            isEliminated = true; // Bị bắt, loại bỏ
            UpdateVisuals();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdatePlayerCounts();
            }

            // Stop movement
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            if (pv.IsMine)
            {
                Debug.Log("You have been eliminated!");
            }
        }

        [PunRPC]
        public void SetAsHuman()
        {
            isGhost = false;
            isEliminated = false;
            UpdateVisuals();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdatePlayerCounts();
            }

            if (pv.IsMine)
            {
                Debug.Log("You are human!");
            }
        }

        private void UpdateVisuals()
        {
            Color targetColor = humanColor;

            if (isEliminated)
            {
                targetColor = eliminatedColor; // Trắng mờ
            }
            else if (isGhost)
            {
                targetColor = ghostColor; // Đỏ
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = targetColor;
            }

            // Also update all child sprite renderers
            SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer sr in childRenderers)
            {
                sr.color = targetColor;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!pv.IsMine) return;
            if (isEliminated) return; // Người bị loại không thể tag

            // Check if collided with another player
            PlayerController otherPlayer = collision.GetComponent<PlayerController>();

            if (otherPlayer != null && otherPlayer.photonView != null)
            {
                // If I'm a ghost and other is human (not eliminated), tag them
                if (isGhost && !otherPlayer.IsGhost && !otherPlayer.IsEliminated)
                {
                    TagPlayer(otherPlayer);
                }
            }
        }

        private void TagPlayer(PlayerController target)
        {
            // Call RPC to eliminate the target
            target.photonView.RPC("SetAsEliminated", RpcTarget.AllBuffered);

            Debug.Log("Tagged player: " + target.photonView.Owner.NickName);
        }
    }
}
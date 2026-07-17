using UnityEngine;

namespace VirtualRoom.Audience.Unity
{
    [AddComponentMenu("Virtual Room/Audience Member View")]
    public class AudienceMemberView : MonoBehaviour
    {
        [Header("Rig (all optional)")]
        public Transform body;
        public Transform head;
        public Transform leftArm;
        public Transform rightArm;
        public Transform popupAnchor;

        [Header("Animation")]
        [Tooltip("Play reaction animations by moving the rig above. Turn off if an Animator drives everything.")]
        public bool proceduralAnimation = true;

        [Range(0f, 2f)]
        [Tooltip("How much they fidget and breathe while doing nothing in particular.")]
        public float idleAmount = 1f;

        public AudienceMember Member { get; internal set; }

        private AudienceRoomBehaviour _room;
        private Animator _animator;
        private Camera _camera;

        private Vector3 _bodyRest, _headRest;
        private Quaternion _bodyRestRot, _headRestRot, _leftArmRest, _rightArmRest;

        private float _phase;
        private float _slump;

        private void Awake()
        {
            _room = GetComponentInParent<AudienceRoomBehaviour>();
            _animator = GetComponentInChildren<Animator>();

            if (body != null) { _bodyRest = body.localPosition; _bodyRestRot = body.localRotation; }
            if (head != null) { _headRest = head.localPosition; _headRestRot = head.localRotation; }
            if (leftArm != null) _leftArmRest = leftArm.localRotation;
            if (rightArm != null) _rightArmRest = rightArm.localRotation;

            _phase = Random.Range(0f, 100f);
        }

        internal void OnReacted(ReactionType reaction, float intensity)
        {
            if (popupAnchor != null)
            {
                SpawnPopup(reaction, intensity);
            }

            if (_animator != null)
            {
                _animator.SetTrigger(reaction.ToString());
            }
        }

        private void SpawnPopup(ReactionType reaction, float intensity)
        {
            if (_room == null || !_room.showReactionPopups) return;

            ReactionPopup popup = _room.CreatePopup(popupAnchor.position);
            if (popup == null) return;

            if (_camera == null) _camera = _room.viewer != null ? _room.viewer : Camera.main;
            popup.Play(reaction, intensity, _camera);
        }

        private void Update()
        {
            if (Member == null || !proceduralAnimation) return;

            float dt = Time.deltaTime;
            _phase += dt;

            _slump = Mathf.Lerp(_slump, 1f - Member.Attention, 1f - Mathf.Exp(-2f * dt));

            ApplyIdle();

            if (Member.IsReacting)
            {
                ApplyReaction(Member.CurrentReaction, Member.ReactionIntensity, ReactionProgress());
            }
        }

        private float ReactionProgress()
        {
            float total = Reactions.DurationOf(Member.CurrentReaction);
            if (total <= 0f) return 1f;
            return Mathf.Clamp01(1f - Member.ReactionTimeLeft / total);
        }

        private void ApplyIdle()
        {
            float breath = Mathf.Sin(_phase * 1.6f) * 0.012f * idleAmount;
            float sway = Mathf.Sin(_phase * 0.7f) * 2.5f * idleAmount;

            if (body != null)
            {
                body.localPosition = _bodyRest + new Vector3(0f, breath, 0f);
                body.localRotation = _bodyRestRot * Quaternion.Euler(_slump * 12f, sway * _slump, sway * 0.5f);
            }

            if (head != null)
            {
                head.localPosition = _headRest + new Vector3(0f, breath * 0.5f, 0f);
                head.localRotation = _headRestRot * Quaternion.Euler(_slump * 22f, sway * 1.5f, 0f);
            }
        }

        private void ApplyReaction(ReactionType reaction, float intensity, float t)
        {
            float envelope = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            float amount = envelope * Mathf.Lerp(0.6f, 1f, intensity);

            switch (reaction)
            {
                case ReactionType.Applause:
                {
                    float clap = (Mathf.Sin(_phase * 32f) + 1f) * 0.5f;
                    ArmsTogether(amount, clap);
                    Bob(amount * 0.02f, 6f);
                    break;
                }

                case ReactionType.Cheer:
                {
                    ArmsUp(amount, spread: 35f);
                    Bob(amount * 0.06f, 7f);
                    HeadPitch(-amount * 12f);
                    break;
                }

                case ReactionType.Laugh:
                {
                    float rock = Mathf.Sin(_phase * 14f) * amount * 7f;
                    BodyLean(-amount * 8f + rock * 0.4f);
                    HeadPitch(-amount * 16f);
                    Bob(amount * 0.025f, 12f);
                    break;
                }

                case ReactionType.Nod:
                {
                    HeadPitch(Mathf.Sin(_phase * 8f) * amount * 14f);
                    break;
                }

                case ReactionType.Wow:
                {
                    BodyLean(-amount * 10f);
                    HeadPitch(-amount * 10f);
                    ArmsUp(amount * 0.35f, spread: 55f);
                    break;
                }

                case ReactionType.Love:
                {
                    ArmsTogether(amount * 0.8f, 0.2f);
                    HeadRoll(Mathf.Sin(_phase * 2f) * amount * 10f);
                    break;
                }

                case ReactionType.ThumbsUp:
                {
                    RaiseRightArm(amount, pitch: -35f);
                    HeadPitch(amount * 5f);
                    break;
                }

                case ReactionType.Question:
                {
                    float raise = Mathf.Clamp01(t * 6f);
                    RaiseRightArm(raise, pitch: -150f, roll: 14f);
                    break;
                }

                case ReactionType.Confused:
                {
                    HeadRoll(amount * 18f);
                    HeadPitch(amount * 6f);
                    break;
                }

                case ReactionType.Yawn:
                {
                    HeadPitch(-amount * 25f);
                    ArmsUp(amount * 0.7f, spread: 60f);
                    BodyLean(-amount * 5f);
                    break;
                }

                case ReactionType.Distracted:
                {
                    HeadYaw(amount * 40f);
                    HeadPitch(amount * 25f);
                    BodyLean(amount * 6f);
                    break;
                }
            }
        }

        private void Bob(float height, float speed)
        {
            if (body == null) return;
            body.localPosition += new Vector3(0f, Mathf.Abs(Mathf.Sin(_phase * speed)) * height, 0f);
        }

        private void BodyLean(float degrees)
        {
            if (body == null) return;
            body.localRotation *= Quaternion.Euler(degrees, 0f, 0f);
        }

        private void HeadPitch(float degrees)
        {
            if (head == null) return;
            head.localRotation *= Quaternion.Euler(degrees, 0f, 0f);
        }

        private void HeadYaw(float degrees)
        {
            if (head == null) return;
            head.localRotation *= Quaternion.Euler(0f, degrees, 0f);
        }

        private void HeadRoll(float degrees)
        {
            if (head == null) return;
            head.localRotation *= Quaternion.Euler(0f, 0f, degrees);
        }

        private void ArmsTogether(float amount, float clap)
        {
            float inward = Mathf.Lerp(0f, 32f, amount) + clap * 10f * amount;
            float lift = Mathf.Lerp(0f, -30f, amount);

            if (leftArm != null) leftArm.localRotation = _leftArmRest * Quaternion.Euler(lift, 0f, inward);
            if (rightArm != null) rightArm.localRotation = _rightArmRest * Quaternion.Euler(lift, 0f, -inward);
        }

        private void ArmsUp(float amount, float spread)
        {
            float pitch = Mathf.Lerp(0f, -110f, amount);
            float roll = Mathf.Lerp(0f, spread, amount);

            if (leftArm != null) leftArm.localRotation = _leftArmRest * Quaternion.Euler(pitch, 0f, -roll);
            if (rightArm != null) rightArm.localRotation = _rightArmRest * Quaternion.Euler(pitch, 0f, roll);
        }

        private void RaiseRightArm(float amount, float pitch, float roll = 0f)
        {
            if (rightArm == null) return;

            rightArm.localRotation = _rightArmRest * Quaternion.Euler(
                Mathf.Lerp(0f, pitch, amount), 0f, Mathf.Lerp(0f, roll, amount));
        }
    }
}

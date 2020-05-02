﻿using System.Collections;
using UnityEngine;

public class TrappedPlayer : MonoBehaviour
{
   [Header("Move")]
   public float horizontalSpeed = 400f;
   [Range(0, .3f)] public float moveSmoothing = 0.05f;
   private Vector3 velocity = Vector3.zero;

   [Header("Jump")]
   public float jumpForce = 400f;
   public int maxJumpNumber = 2;
   public int JumpNumber { get; private set; }
   private Color noJumpColor = Color.black;
   private Color[] jumpColors;
   public float goDownVelocity = 30f;
   public float goDownMaxVelocity = 12f;

   [Header("Wall")]
   public float frictionForce = 4f;

   [Header("Dash")]
   public float dashSpeed = 1000f;
   public float dashDur = 0.2f;
   public float dashReload = 10f;
   public float destroyingTime = 3f;
   public bool CanDash { get; private set; } = true;
   private Color dashColor = Color.white;

   [Header("Death")]
   public float deathDur = 3f;
   public float respawnDur = 1.5f;
   public float blinkFreq = 0.1f;
   public bool CanDie { get; private set; } =  true;
   public bool CollidedDanger { get; set; } = false;
   private Transform respawn;

   [Header("Check")]
   public LayerMask whatIsGround;
   private float xBoxCheckerSize = 0.7f;
   private float yBoxCheckerSize = 0.7f;
   private Vector3 yBoxChecker;
   private Vector3 xBoxChecker;
   private Transform groundCheck;
   private Transform ceilingCheck;
   private Transform leftCheck;
   private Transform rightCheck;

   //Others
   public Rigidbody Rigidbody { get; private set; }
   public Collider Collider { get; private set; }
   public Renderer Renderer { get; private set; }

   private void Awake()
   {
      //Others
      Rigidbody = GetComponent<Rigidbody>();
      Collider = GetComponent<BoxCollider>();
      Renderer = GetComponent<Renderer>();

      //Jump
      JumpNumber = maxJumpNumber;
      jumpColors = new Color[maxJumpNumber + 1];
      for (int i = 0; i < jumpColors.Length; i++)
      {
         jumpColors[i] = Color.Lerp(noJumpColor, Renderer.material.color, (float)i / maxJumpNumber);
      }

      //Death
      respawn = GameObjectUtils.Find("Respawn").GetComponent<Transform>();

      //Check
      xBoxChecker = new Vector3(0f, xBoxCheckerSize / 2, xBoxCheckerSize / 2);
      xBoxChecker.Scale(transform.localScale);
      yBoxChecker = new Vector3(yBoxCheckerSize / 2, 0f, yBoxCheckerSize / 2);
      yBoxChecker.Scale(transform.localScale);
      groundCheck = GameObjectUtils.Find("GroundCheck").GetComponent<Transform>();
      ceilingCheck = GameObjectUtils.Find("CeilingCheck").GetComponent<Transform>();
      leftCheck = GameObjectUtils.Find("LeftCheck").GetComponent<Transform>();
      rightCheck = GameObjectUtils.Find("RightCheck").GetComponent<Transform>();

      Respawn();
   }

   #region Move
   public void MoveHorizontal(float direction, float moveModificator=1f)
   {
      Vector3 targetVelocity = new Vector2(moveModificator * direction * horizontalSpeed * Time.fixedDeltaTime, Rigidbody.velocity.y);
      Rigidbody.velocity = Vector3.SmoothDamp(Rigidbody.velocity, targetVelocity, ref velocity, moveSmoothing);
   }
   #endregion

   #region Jump
   public void Jump(float jumpModificator=1f)
   {
      if (JumpNumber > 0)
      {
         Rigidbody.velocity = new Vector3(Rigidbody.velocity.x, 0f, Rigidbody.velocity.z);
         Rigidbody.AddForce(new Vector2(0f, jumpModificator * jumpForce));
         JumpNumber--;
      }
      Renderer.material.color = jumpColors[JumpNumber];
   }

   public void ResetJump()
   {
      JumpNumber = maxJumpNumber;
      ResetColor();
   }

   public void ResetColor()
   {
      Renderer.material.color = jumpColors[JumpNumber];
   }
   #endregion

   #region Dash
   public void Dash(float direction)
   {
      Rigidbody.velocity = dashSpeed * Time.fixedDeltaTime * new Vector3(direction, 0f, 0f);
   }

   public void ReloadDash()
   {
      CanDash = false;
      StartCoroutine(ReloadDashRoutine());
   }

   private IEnumerator ReloadDashRoutine()
   {
      yield return new WaitForSeconds(dashReload);
      CanDash = true;
   } 

   public void SetDashColor()
   {
      Renderer.material.color = dashColor;
   }

   public void MakeDangerHarmless(GameObject gameObject)
   {
      if (gameObject.CompareTag("Danger"))
      {
         gameObject.tag = "Untagged";
         gameObject.GetComponent<Renderer>().material.color = dashColor;
         Destroy(gameObject, destroyingTime);
      }
   }
   #endregion

   #region Death
   public void Respawn()
   {
      StartCoroutine(RespawnRoutine());
   }

   private IEnumerator RespawnRoutine()
   {
      transform.position = respawn.position;
      transform.rotation = Quaternion.Euler(Vector3.zero);
      CanDie = true;
      IEnumerator blinkinRoutine = BlinkingRoutine();
      StartCoroutine(blinkinRoutine);
      yield return new WaitForSeconds(respawnDur);
      CanDie = true;
      StopCoroutine(blinkinRoutine);
      Renderer.enabled = true;
   }

   private IEnumerator BlinkingRoutine()
   {
      while (true)
      {
         yield return new WaitForSeconds(blinkFreq);
         Renderer.enabled = !Renderer.enabled;
      }
   }
   #endregion

   #region Check
   private void OnCollisionEnter(Collision collision)
   {
      if (collision.gameObject.CompareTag("Danger") && CanDie) { CollidedDanger = true; }
   }

   private void OnCollisionExit(Collision collision)
   {
      if (collision.gameObject.CompareTag("Danger")) { CollidedDanger = false; }
   }

   private bool CollisionCheck(Vector3 position, Vector3 boxChecker)
   {
      Collider[] colliders = Physics.OverlapBox(position, boxChecker, Quaternion.identity, whatIsGround);
      foreach (Collider collider in colliders)
      {
         if (collider.gameObject != gameObject)
         {
            return true;
         }
      }

      return false;
   }

   public bool GroundCheck()
   {
      return CollisionCheck(groundCheck.position, yBoxChecker);
   }

   public bool CeilingCheck()
   {
      return CollisionCheck(ceilingCheck.position, yBoxChecker);
   }

   public bool LeftCheck()
   {
      return CollisionCheck(leftCheck.position, xBoxChecker);
   }

   public bool RightCheck()
   {
      return CollisionCheck(rightCheck.position, xBoxChecker);
   }
   #endregion
}
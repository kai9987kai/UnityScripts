// This script goes on the Ball prefab. It's the ball's little brain!
// Trying again to figure out why targets aren't always getting destroyed.
// Date: [Current Date, e.g., May 8, 2025]
using UnityEngine;
using System.Collections;

public class BallController : MonoBehaviour
{
    [Header("How Long The Ball Lasts")]
    public float max_Ball_Lifetime_Seconds = 10f;

    [Header("KillZone Settings - Important!")]
    public float grace_Period_Before_KillZone_Active = 0.5f;

    private float current_Ball_Life_Timer_Value;
    private bool can_This_Ball_Trigger_A_KillZone_Now = false;
    private bool is_This_Ball_Already_Marked_As_Dead = false;

    // Start is called by Unity just once, right at the beginning for this ball.
    void Start()
    {
        // Check the tag, just in case.
        if (!CompareTag("Ball")) { UnityEngine.Debug.LogWarning($"Ball '{gameObject.name}': My tag isn't 'Ball'! This might cause problems?", this); }
        current_Ball_Life_Timer_Value = max_Ball_Lifetime_Seconds;
        StartCoroutine(Routine_To_Enable_KillZone_Trigger_After_Delay());
    }

    // Update is called by Unity every single frame. Wow!
    void Update()
    {
        if (is_This_Ball_Already_Marked_As_Dead) return; // If already dead, do nothing more.

        current_Ball_Life_Timer_Value -= Time.deltaTime; // Countdown timer.

        if (current_Ball_Life_Timer_Value <= 0f) { Make_This_Ball_Die_Now("Timeout (lifetime ran out)"); }
        else if (transform.position.y < -50f) { Make_This_Ball_Die_Now("Fell out of the world (too low!)"); }
    }
    // This is a "Coroutine". It can pause itself.
    IEnumerator Routine_To_Enable_KillZone_Trigger_After_Delay()
    {
        can_This_Ball_Trigger_A_KillZone_Now = false;
        yield return new WaitForSeconds(grace_Period_Before_KillZone_Active);
        can_This_Ball_Trigger_A_KillZone_Now = true;
    }

    // This function is called by Unity whenever this ball's collider bumps into another solid collider.
    // LET'S REALLY WATCH THIS ONE FOR TARGETS!
    void OnCollisionEnter(Collision the_Collision_Info)
    {
        if (is_This_Ball_Already_Marked_As_Dead) return; // Ignore if already dead.

        GameObject what_The_Ball_Actually_Hit = the_Collision_Info.gameObject; // What EXACTLY did I hit?
        Collider what_Collider_I_Hit = the_Collision_Info.collider; // Which specific collider part?

        // Log EVERY collision now, to be certain.
        Debug.Log($"BALL COLLISION on '{gameObject.name}': I just hit GameObject named '{what_The_Ball_Actually_Hit.name}' (Tag: '{what_The_Ball_Actually_Hit.tag}', Layer: {LayerMask.LayerToName(what_The_Ball_Actually_Hit.layer)}). The specific collider hit was '{what_Collider_I_Hit.name}'.", this);

        // Do I have my GameManager friend?
        if (GameManager.game_Instance == null)
        {
            Debug.LogError($"BALL COLLISION PROBLEM for '{gameObject.name}': Can't find GameManager when hitting '{what_The_Ball_Actually_Hit.name}'! Cannot process target hits!", this);
            return;
        }

        // --- Check for Blue Target ---
        // Using CompareTag is usually best, but let's be super careful and check the name too just for debugging? No, stick to tags.
        if (what_The_Ball_Actually_Hit.CompareTag("BlueTarget"))
        {
            Debug.Log($"BALL '{gameObject.name}': Confirmed HIT on object tagged 'BlueTarget' named '{what_The_Ball_Actually_Hit.name}'. Telling GameManager and attempting Destroy...", this);
            GameManager.game_Instance.ProcessTargetHit("Blue"); // Tell GameManager (it handles score/count)
            Destroy(what_The_Ball_Actually_Hit); // Destroy the specific GameObject I hit.
            Debug.Log($"BALL '{gameObject.name}': --- Sent DESTROY command for '{what_The_Ball_Actually_Hit.name}'. Did it disappear from the scene/hierarchy? ---", this);
            Make_This_Ball_Die_Now("Hit a Blue Target"); // Ball is used up.
        }
        // --- Check for Green Target ---
        else if (what_The_Ball_Actually_Hit.CompareTag("GreenTarget"))
        {
            Debug.Log($"BALL '{gameObject.name}': Confirmed HIT on object tagged 'GreenTarget' named '{what_The_Ball_Actually_Hit.name}'. Telling GameManager (via BallDied) and attempting Destroy...", this);
            // GameManager gets told indirectly via the reason string in BallDied for green targets.
            Destroy(what_The_Ball_Actually_Hit); // Destroy the specific GameObject I hit.
            Debug.Log($"BALL '{gameObject.name}': --- Sent DESTROY command for '{what_The_Ball_Actually_Hit.name}'. Did it disappear from the scene/hierarchy? ---", this);
            Make_This_Ball_Die_Now("Hit Green Target"); // Ball is used up, this reason gives shots in GM.
        }
        // --- Add other things the ball might hit that should destroy the ball ---
        else if (what_The_Ball_Actually_Hit.CompareTag("Obstacle"))
        {
            Debug.Log($"BALL '{gameObject.name}': Hit an Obstacle ('{what_The_Ball_Actually_Hit.name}'). Ball dies, obstacle stays.", this);
            Make_This_Ball_Die_Now("Hit an Obstacle");
        }
        else if (what_The_Ball_Actually_Hit.CompareTag("Ground")) // Or whatever your ground/platforms are tagged
        {
            // Depending on your game, maybe hitting the ground should just make the ball stop, or maybe it dies.
            // For now, let's assume it just stops or bounces (do nothing here).
            // If hitting ground should kill the ball, uncomment the next line:
            // Make_This_Ball_Die_Now("Hit the Ground");
        }
        else
        {
            Debug.Log($"BALL '{gameObject.name}': Hit something else ('{what_The_Ball_Actually_Hit.name}' with tag '{what_The_Ball_Actually_Hit.tag}') that isn't a special target or obstacle I know about.", this);
            // Decide if the ball should die when hitting unknown things. If so:
            // Make_This_Ball_Die_Now("Hit something unknown");
        }
    }

    // This function is called by Unity if this ball's collider enters a "Trigger" collider.
    void OnTriggerEnter(Collider the_Other_Collider_It_Entered)
    {
        if (is_This_Ball_Already_Marked_As_Dead || !can_This_Ball_Trigger_A_KillZone_Now) return;

        if (the_Other_Collider_It_Entered.CompareTag("KillZone"))
        {
            Debug.Log($"BALL '{gameObject.name}': Entered KillZone trigger '{the_Other_Collider_It_Entered.name}'. Dying now.", this);
            Make_This_Ball_Die_Now("Entered a KillZone");
        }
    }

    // This is my own function that handles everything when the ball needs to "die".
    private void Make_This_Ball_Die_Now(string how_The_Ball_Died_Reason)
    {
        if (is_This_Ball_Already_Marked_As_Dead) return; // Only run once!
        is_This_Ball_Already_Marked_As_Dead = true;

        Debug.Log($"BALL '{gameObject.name}': Processing DEATH. Reason: {how_The_Ball_Died_Reason}. Telling GameManager (if possible) and calling Destroy(gameObject).", this);

        if (GameManager.game_Instance != null)
        {
            GameManager.game_Instance.BallDied(how_The_Ball_Died_Reason);
        }
        else
        {
            Debug.LogError($"BALL DEATH PROBLEM for '{gameObject.name}': GameManager is NULL! Can't report death reason '{how_The_Ball_Died_Reason}'.", this);
        }

        Destroy(gameObject); // Destroy the ball itself.
    }
}
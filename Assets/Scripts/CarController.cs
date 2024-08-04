using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEngine.ParticleSystem;

public class CarController : MonoBehaviour
{
    public float maxSpeed = 10000f;
    public float acceleration = 10f;
    public float maxSteeringAngle = 30f; // Maximum steering angle at low speed
    public float minSteeringAngle = 10f; // Minimum steering angle at high speed
    public float[] gearRatios = { 0.5f, 1f, 1.5f, 2f };
    public float shiftCooldown = 5f; // 5 seconds to recharge shift mode
    public float maxShiftDuration = 3f; // Max 3 seconds in shift mode
    public AudioSource engineSoundSource;
    public AudioClip[] engineSounds; // Different engine sounds for different gears
    public Animator carAnimator; // Placeholder for shift animation
    public float gearShiftPauseDuration = 0.1f; // Pause duration during gear shift
    public float steeringParabolaCoefficient = 0.5f; // Controls the shape of the parabola
    public float peakSpeedRatio = 0.25f; // Ratio of max speed where steering angle peaks
    public float driftFactor = 0.1f; // How much speed affects drift behavior
    public float maxDriftAngle = 45f; // Maximum angle during drift
    public float driftTurnMultiplier = 2f; // Additional turn multiplier during drift
    public float driftSpeedReductionRate = 10f; // Speed reduction rate while drifting
    public Material DimensionMaterial;
    public TrailRenderer[] TireTrails;
    public GameObject ShiftParticleFX;
    public VolumeProfile ppProfile;

    private Rigidbody rb;
    private float currentSpeed = 0f;
    private bool isShifting = false;
    private float shiftTimer = 0f;
    private float shiftCooldownTimer = 0f;
    private bool canShift = true;
    private bool isGearShifting = false;
    private bool isDrifting = false;
    private bool isBoosting = false;
    private float driftStartTime = 0f;
    private float driftAngle = 0f;
    private float baseAcceleration;
    private List<Material[]> materials = new();
    private List<Transform> carParts = new();
    private GameObject particles;
    private ColorAdjustments colorAdjustments;
    private LensDistortion lensDistortion;
    private Color overlayColorEnd = new Color(2.000f, 0.802f, 1.869f, 1.000f);
    private Color overlayColorStart = new Color(0.700f, 0.3802f, 0.5869f, 1.000f);

    void Start()
    {
        baseAcceleration = acceleration;
        rb = GetComponent<Rigidbody>();
        materials.Add(transform.GetComponent<MeshRenderer>().materials);
        ppProfile.TryGet(out lensDistortion);
        ppProfile.TryGet(out colorAdjustments);
        foreach (Transform child in transform)
        {
            if (!child.name.Equals("disintegrate") && !child.name.Equals("CameraPP") && !child.name.Equals("CollisionTarget"))
            {
                carParts.Add(child);
                materials.Add(child.GetComponent<MeshRenderer>().materials);
            }
        }
        particles = Instantiate(ShiftParticleFX);
        particles.transform.SetParent(transform);
        particles.transform.position = transform.position + new Vector3(0, 0, 50);
        particles.transform.Rotate(0, 180f, 0);
    }

    void Update()
    {
        HandleSteering();
        HandleInput();
        UpdateEngineSound();
        UpdateShiftMode();
        Corrector();
    }

    void HandleSteering()
    {
        float turnInput = Input.GetAxis("Horizontal"); // A/D or Left/Right arrows

        // Calculate base steering angle
        float speedFactor = currentSpeed / maxSpeed;
        float parabolaFactor = 1 - Mathf.Pow((speedFactor * 2 - 1) / peakSpeedRatio, 2);
        float targetSteeringAngle = Mathf.Lerp(minSteeringAngle, maxSteeringAngle, parabolaFactor);
        float steerAngle = turnInput * targetSteeringAngle * Time.deltaTime;

        if (!isDrifting && currentSpeed > 0)
        {
            // Apply normal steering angle
            transform.Rotate(Vector3.up, steerAngle);

            // Store the current drift angle for use during drift
            driftAngle = steerAngle;
        }
        else if (isDrifting)
        {
            // Add additional turn during drift
            //float additionalTurn = driftAngleToApply * driftTurnMultiplier;
            float additionalTurn = maxDriftAngle * turnInput;
            transform.Rotate(Vector3.up, additionalTurn * Time.deltaTime);

            // Reduce speed while drifting
            currentSpeed -= driftSpeedReductionRate * Time.deltaTime;
            currentSpeed = Mathf.Max(currentSpeed, 0f); // Ensure speed doesn't go below zero
        }
    }

    void HandleInput()
    {
        if (!isGearShifting)
        {
            float forwardInput = Input.GetAxis("Vertical"); // W/S or Up/Down arrows

            if (forwardInput > 0)
            {
                currentSpeed += acceleration  * Time.deltaTime;
            }
            else if (forwardInput < 0)
            {
                currentSpeed -= acceleration * Time.deltaTime;
            }
            else
            {
                currentSpeed = Mathf.Lerp(currentSpeed, 0, Time.deltaTime * acceleration/10);
            }

            currentSpeed = Mathf.Clamp(currentSpeed, 0, maxSpeed);

            Vector3 forward = transform.forward * currentSpeed;
            rb.velocity = forward;

            HandleDriftInput();
        }
        if (Input.GetKeyDown(KeyCode.LeftShift) && canShift)
        {
            StartShift();
        }
        if (Input.GetKeyUp(KeyCode.LeftShift) && !canShift)
        {
            EndShift();
        }
    }


    void UpdateEngineSound()
    {
        engineSoundSource.clip = engineSounds[0];
        engineSoundSource.pitch = Mathf.Lerp(0.5f, 1.5f, currentSpeed / maxSpeed);
        if (!engineSoundSource.isPlaying)
        {
            engineSoundSource.Play();
        }
    }

    void UpdateShiftMode()
    {
        if (isShifting)
        {
            shiftTimer -= Time.deltaTime;
            colorAdjustments.colorFilter.value = ChangeColorTowards(colorAdjustments.colorFilter.value, overlayColorEnd, 0.003f);
            if (shiftTimer <= 0)
            {
                EndShift();
            }
        }
        else
        {
            if (shiftCooldownTimer > 0)
            {
                shiftCooldownTimer -= Time.deltaTime;
            }
            else if (!canShift)
            {
                canShift = true;
            }
        }
    }

    void StartShift()
    {
        isShifting = true;
        canShift = false;
        transform.Find("CollisionTarget").GetComponent<BoxCollider>().enabled = true;
        transform.Find("CameraPP").gameObject.SetActive(isShifting);
        colorAdjustments.colorFilter.value = overlayColorStart;
        shiftTimer = maxShiftDuration;
        transform.Find("disintegrate").GetComponent<SkinnedMeshRenderer>().enabled = true;
        carAnimator.SetTrigger("Disolve");
        carAnimator.speed = 4.5f;
        transform.GetComponent<MeshRenderer>().material = DimensionMaterial;
        List<Material> AssignedMaterials = new();
        for (int i = 0; i < carParts.Count; i++)
        {
            
            if (carParts[i].GetComponent<MeshRenderer>().materials.Length > 1)
            {
                for (int i2 = 0; i2 < carParts[i].GetComponent<MeshRenderer>().materials.Length; i2++)
                {
                    AssignedMaterials.Add(DimensionMaterial);
                }
                carParts[i].GetComponent<MeshRenderer>().materials = AssignedMaterials.ToArray();

            }
            else
            {
                carParts[i].GetComponent<MeshRenderer>().material = DimensionMaterial;
            }
        }
        gameObject.layer = 3;
    }

    public void PauseDisentegrate()
    {
        print("Paused");
        particles.SetActive(isShifting);
        if (isShifting)
        {
            foreach (TrailRenderer trail in TireTrails)
            {
                trail.emitting = true;
            }
            carAnimator.speed = 0;
        }
    }

    public void HideDisentegrate()
    {
        transform.Find("disintegrate").GetComponent<SkinnedMeshRenderer>().enabled = false;
        transform.GetComponent<MeshRenderer>().materials = materials[0];

        for (int i = 0; i < carParts.Count; i++)
        {
            carParts[i].GetComponent<MeshRenderer>().materials = materials[i+1];
        } 
    }

    void EndShift()
    {
        isShifting = false;
        if (!isBoosting)
        {
            particles.SetActive(isShifting);
            transform.Find("CameraPP").gameObject.SetActive(isShifting);
            foreach (TrailRenderer trail in TireTrails)
            {
                trail.emitting = false;
            }
        }
        carAnimator.speed = 4.5f;
        shiftCooldownTimer = shiftCooldown;
        transform.Find("CollisionTarget").GetComponent<BoxCollider>().enabled = false;
        gameObject.layer = 0;
    }

    void HandleDriftInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) && !isDrifting)
        {
            StartDrift();
        }

        if (isDrifting)
        {
            float driftTime = Time.time - driftStartTime;
            float driftDecay = Mathf.Exp(-driftFactor * driftTime);

            // Apply drift angle with decay
            float driftAngleDecay = driftAngle * driftDecay;

            // Apply decayed drift angle
            transform.Rotate(Vector3.up, driftAngleDecay * Time.deltaTime);

            // Reduce speed while drifting
            currentSpeed -= driftSpeedReductionRate * Time.deltaTime;
            currentSpeed = Mathf.Max(currentSpeed, 0f); // Ensure speed doesn't go below zero

            // End drift if control key is released or drift decay is very low
            if (!Input.GetKey(KeyCode.LeftControl) || driftDecay < 0.01f)
            {
                EndDrift();
            }
        }
    }

    void StartDrift()
    {
        isDrifting = true;
        driftStartTime = Time.time;
        //carAnimator.SetTrigger("Drift"); // Placeholder for drift animation
    }

    void EndDrift()
    {
        isDrifting = false;
        //carAnimator.SetTrigger("EndDrift"); // Placeholder for end drift animation
    }

    Color ChangeColorTowards(Color current, Color target, float increment)
    {
        if(current != target)
        {
            float r = Mathf.MoveTowards(current.r, target.r, increment);
            float g = Mathf.MoveTowards(current.g, target.g, increment);
            float b = Mathf.MoveTowards(current.b, target.b, increment);
            float a = Mathf.MoveTowards(current.a, target.a, increment);

            return new Color(r, g, b, a);
        }
        return current;
    }
    private void Corrector()
    {
        if (colorAdjustments.saturation.value != 0f)
        {
            colorAdjustments.saturation.value = Mathf.MoveTowards(colorAdjustments.saturation.value, 0, 0.25f);
        }
        if (colorAdjustments.contrast.value != 0f)
        {
            colorAdjustments.contrast.value = Mathf.MoveTowards(colorAdjustments.contrast.value, 0, 0.8f);
        }
        if (lensDistortion.intensity.value != 0f)
        {
            lensDistortion.intensity.value = Mathf.MoveTowards(lensDistortion.intensity.value, 0, 0.01f);
        }
    }

    public IEnumerator SpeedBoost()
    {
        isBoosting = true;
        acceleration = acceleration * 1.5f;
        lensDistortion.intensity.value = -2f;
        colorAdjustments.contrast.value = 100f;
        colorAdjustments.saturation.value = -100f;

        yield return new WaitForSeconds(1);
        isBoosting = false;
        acceleration = baseAcceleration;
        if (!isShifting)
        {
            particles.SetActive(false);
            transform.Find("CameraPP").gameObject.SetActive(false);
            foreach (TrailRenderer trail in TireTrails)
            {
                trail.emitting = false;
            }
        }
    }
}

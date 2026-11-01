using UnityEngine;

/// <summary>
/// Base for an ambient main-menu effect that lives on its own full-size layer.
/// The layer fades in after a delay (so the logo intro plays on a calm screen),
/// then <see cref="Tick"/> runs every frame on unscaled time.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public abstract class MenuFxLayer : MonoBehaviour
{
    [Header("Appear")]
    [SerializeField] private float appearDelay = 1.2f;
    [SerializeField] private float appearTime = 1.5f;

    private CanvasGroup group;
    private float age;

    protected RectTransform Layer => (RectTransform)transform;
    protected float Elapsed { get; private set; }

    protected virtual void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = group.blocksRaycasts = false;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        age += dt;
        group.alpha = FxKit.Smooth(Mathf.Clamp01((age - appearDelay) / appearTime));
        if (age < appearDelay) return;
        Elapsed += dt;
        Tick(dt);
    }

    protected abstract void Tick(float dt);
}

using UnityEngine;

/// <summary>
/// In order for the SingletonScriptableObject to work, the singleton instance must
/// be mapped into this component. Otherwise Unity won't include the
/// asset in the build (singleton will work only in editor).
/// </summary>
public class ScriptableObjectSingletons : MonoBehaviour
{
	//put all singletons here and assign them in editor.
	public ItemTypeToTraitMapping ItemTypeToTraitMapping;
	public CommonTraits CommonTraits;
	public OccupationList OccupationList;
	public BestSlotForTrait BestSlotForTrait;
}

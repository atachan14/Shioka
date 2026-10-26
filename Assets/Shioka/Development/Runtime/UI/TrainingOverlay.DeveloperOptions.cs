#if UNITY_EDITOR || DEVELOPMENT_BUILD
public sealed partial class TrainingOverlay {
 partial void CreateDeveloperOptions(){CreateLocomotionOptions();CreateComparisonOptions();}
 partial void LayoutDeveloperOptions(float w,float h,float left,float x,float right){LayoutLocomotionOptions(w,h,left,x,right);LayoutComparisonOptions(w,h,left,x,right);}
 partial void HideDeveloperOptions(){OptionLocomotionVisible=OptionComparisonVisible=DeveloperOptionVisible=false;if(locomotionOptions!=null)locomotionOptions.SetActive(false);if(comparisonOptions!=null)comparisonOptions.SetActive(false);}
}
#endif

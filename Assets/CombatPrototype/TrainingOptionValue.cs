using System;using UnityEngine;using UnityEngine.UI;using UnityEngine.EventSystems;
// Transactional numeric edit: changing draft never mutates live settings.
public sealed class TrainingOptionValue:MonoBehaviour,IDeselectHandler {
 public bool Editing{get;private set;}public int Draft{get;private set;}Func<int> read;Action<int> apply;int minimum,maximum;string label;Text text;
 public void Configure(string name,int min,int max,Func<int> getter,Action<int> setter){label=name;minimum=min;maximum=max;read=getter;apply=setter;text=GetComponentInChildren<Text>();GetComponent<Button>().onClick.AddListener(Begin);Refresh();}
 public void Begin(){if(Editing)return;Draft=read();Editing=true;if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(gameObject);Refresh();}
 public void Adjust(int amount){if(!Editing)return;Draft=Mathf.Clamp(Draft+amount,minimum,maximum);Refresh();}
 public void Commit(){if(!Editing)return;apply(Draft);Editing=false;Refresh();}
 public void Cancel(){Editing=false;Refresh();}
 public void Refresh(){if(text!=null&&read!=null)text.text=label+" "+(Editing?Draft:read())+"%"+(Editing?"  [draft: J save / K cancel]":"  [J edit]");}
 public void OnDeselect(BaseEventData data){if(Editing)Cancel();}
 void OnDisable(){if(Editing)Cancel();}
}

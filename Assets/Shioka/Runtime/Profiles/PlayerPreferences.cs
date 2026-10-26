using System;
[Serializable]public sealed class PlayerPreferences {public int version=1;public string selectedFighterId;public bool Valid=>version==1&&!string.IsNullOrWhiteSpace(selectedFighterId)&&selectedFighterId.Length<=128;}

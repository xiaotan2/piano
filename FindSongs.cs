using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.UI;
using System.Collections.Generic;

public class FindSongs : MonoBehaviour {

   public Button songButton;
   public Button trackButton;
   public GameObject songsContentPanel;
   public GameObject tracksContentPanel;
   public Text leftTrackName;
   public Text rightTrackName;
   public Slider speedSlider;
   public Slider tempoFactorSlider;

   private string currentSongName = "";
   private int currentLeftTrack = -1;
   private int currentRightTrack = -1;

   const int fileHeight = 25;
   
   // Use this for initialization
	void Start () {
      var songsPath = Path.Combine(Application.dataPath, "../Songs");
      var files = new DirectoryInfo(songsPath).GetFiles();
      // set content panel's height based on file size
      var fileSize = files.Length;
      songsContentPanel.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fileSize * fileHeight);
      foreach (var file in files)
      {
         Button songName = Instantiate(songButton) as Button;
         songName.GetComponentInChildren<Text>().text = file.Name;
         songName.transform.SetParent(songsContentPanel.transform, false);
         songName.transform.localScale = Vector3.one;
      }
      // initialize track list if sequence is available
      if (GlobalReference.sequence != null)
      {
         var sequence = GlobalReference.sequence;
         var numTracks = sequence.Count;
         tracksContentPanel.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, numTracks * fileHeight);
         for (int i = 0; i < numTracks; i++)
         {
            Button trackName = Instantiate(trackButton) as Button;
            trackName.GetComponentInChildren<Text>().text = string.Format("Track {0} With {1} events", i, sequence[i].Count);
            trackName.transform.SetParent(tracksContentPanel.transform, false);
            trackName.transform.localScale = Vector3.one;
         }
      }
      
      // Set speed and tempo
      speedSlider.value = GlobalReference.speed;
      tempoFactorSlider.value = GlobalReference.tempoFactor;

      // Reset the left hand and right hand track index
      GlobalReference.leftHandTrackIndex = -1;
      GlobalReference.rightHandTrackIndex = -1;
   }
	
	// Update is called once per frame
	void Update () {
	   if(GlobalReference.chosenSongName != "" && GlobalReference.chosenSongName != currentSongName)
      {
         // clean the tracks if exist
         var buttons = tracksContentPanel.GetComponentsInChildren<Button>();
         foreach(var button in buttons)
         {
            Destroy(button.gameObject);
         }
         
         // reset chosenSongName and set currentSongName
         currentSongName = GlobalReference.chosenSongName;
         GlobalReference.chosenSongName = "";

         // load the song file into sequence
         var songsPath = Path.Combine(Application.dataPath, "../Songs");
         var filePath = Path.Combine(songsPath, currentSongName);
         if (GlobalReference.sequence == null)
         {
            GlobalReference.sequence = new Sequence();
            GlobalReference.sequence.Format = 1;
         }
         GlobalReference.sequence.Load(filePath);

         // populate the track list
         var sequence = GlobalReference.sequence;
         var numTracks = sequence.Count;
         tracksContentPanel.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, numTracks * fileHeight);
         for (int i = 0; i < numTracks; i++)
         {
            Button trackName = Instantiate(trackButton) as Button;
            trackName.GetComponentInChildren<Text>().text = string.Format("Track {0} With {1} events", i, sequence[i].Count);
            trackName.transform.SetParent(tracksContentPanel.transform, false);
            trackName.transform.localScale = Vector3.one;
         }
      }
      if(currentLeftTrack != GlobalReference.leftHandTrackIndex)
      {
         currentLeftTrack = GlobalReference.leftHandTrackIndex;
         if (currentLeftTrack != -1)
            leftTrackName.text = string.Format("Track {0}", currentLeftTrack);
         else
            leftTrackName.text = "Empty";
      }
      if (currentRightTrack != GlobalReference.rightHandTrackIndex)
      {
         currentRightTrack = GlobalReference.rightHandTrackIndex;
         if (currentRightTrack != -1)
            rightTrackName.text = string.Format("Track {0}", currentRightTrack);
         else
            rightTrackName.text = "Empty";
      }
   }

   public void OnSpeedValueChanged(float value)
   {
      Debug.LogFormat("Change Speed to {0}", value);
      GlobalReference.speed = (int)value;
   }

   public void OnTempoFactorValueChanged(float value)
   {
      GlobalReference.tempoFactor = (int)value;
   }
}

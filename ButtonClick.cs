using UnityEngine;
using UnityEngine.UI;

public class ButtonClick : MonoBehaviour {

   public GameObject sequencer;
   public string sceneToLoad;

   public void OnStartClick()
   {
      sequencer.SendMessage("ContinueSequence");
   }

   public void OnStopClick()
   {
      sequencer.SendMessage("StopSequence");
   }

   public void OnRestartClick()
   {
      sequencer.SendMessage("RestartSequence");
   }

   public void songButtonOnClick()
   {
      // put the name of the button into chosen song name
      GlobalReference.chosenSongName = GetComponentInChildren<Text>().text;

      // Reset the left hand and right hand index
      GlobalReference.leftHandTrackIndex = -1;
      GlobalReference.rightHandTrackIndex = -1;
   }

   public void trackButtonOnClick()
   {
      string trackName = GetComponentInChildren<Text>().text;
      int trackIndex = -1;
      if(!System.Int32.TryParse(trackName.Split(' ')[1], out trackIndex))
      {
         throw new UnityException(string.Format("Cannot convert track index {0} to Int32. Track Name is {1}.", trackName.Split(' ')[1], trackName));
      }
      if(GlobalReference.leftHandTrackIndex == -1) // left hand empty
      {
         if (GlobalReference.rightHandTrackIndex == trackIndex) // left hand empty, right hand same
         {
            GlobalReference.rightHandTrackIndex = -1;
            return;
         } // left hand empty, right hand empty or different
         GlobalReference.leftHandTrackIndex = trackIndex;
         return;
      }
      if(GlobalReference.leftHandTrackIndex == trackIndex) // left hand same
      {
         if (GlobalReference.rightHandTrackIndex == -1) // left hand same, right hand empty
            GlobalReference.rightHandTrackIndex = trackIndex;
         // left hand same, right hand different
         GlobalReference.leftHandTrackIndex = -1;
         return;
      }
      if(GlobalReference.rightHandTrackIndex == -1) // left hand different, right hand empty
      {
         GlobalReference.rightHandTrackIndex = trackIndex;
         return;
      }
      if(GlobalReference.rightHandTrackIndex == trackIndex) // left hand different, right hand same
      {
         GlobalReference.rightHandTrackIndex = -1;
         return;
      }
      // left hand different, right hand different
      GlobalReference.leftHandTrackIndex = trackIndex;
   }

   public void playButtonClick()
   {
      UnityEngine.SceneManagement.SceneManager.LoadScene("Scenes/" + sceneToLoad, UnityEngine.SceneManagement.LoadSceneMode.Single);
   }

   public void backButtonClick()
   {
      UnityEngine.SceneManagement.SceneManager.LoadScene("Scenes/ChooseSong", UnityEngine.SceneManagement.LoadSceneMode.Single);
   }

   public void OnPracticeClick(bool practice)
   {
      if(practice)
         Debug.Log("Set Practice to True.");
      else
         Debug.Log("Set Practice to False.");
      GlobalReference.practiceMode = practice;
   }

   public void OnLoopClick(bool loop)
   {
      if (loop)
         Debug.Log("Set Loop to True.");
      else
         Debug.Log("Set Loop to False.");
      GlobalReference.loopMode = loop;
   }

}

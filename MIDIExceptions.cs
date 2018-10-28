using System;
using System.Runtime.InteropServices;
using System.Text;

public class MIDIExceptions : ApplicationException {

   [DllImport("winmm.dll")]
   private static extern int midiInGetErrorText(int errCode,
            StringBuilder errMsg, int sizeOfErrMsg);

   public const int MMSYSERR_ALLOCATED = 4;
   public const int MMSYSERR_BADDB = 14;
   public const int MMSYSERR_BADDEVICEID = 2;
   public const int MMSYSERR_BADERRNUM = 9;
   public const int MMSYSERR_DELETEERROR = 18;
   public const int MMSYSERR_ERROR = 1;
   public const int MMSYSERR_HANDLEBUSY = 12;
   public const int MMSYSERR_INVALFLAG = 10;
   public const int MMSYSERR_INVALHANDLE = 5;
   public const int MMSYSERR_INVALIDALIAS = 13;
   public const int MMSYSERR_INVALPARAM = 11;
   public const int MMSYSERR_KEYNOTFOUND = 15;
   public const int MMSYSERR_LASTERROR = 20;
   public const int MMSYSERR_NODRIVER = 6;
   public const int MMSYSERR_NODRIVERCB = 20;
   public const int MMSYSERR_NOERROR = 0;
   public const int MMSYSERR_NOMEM = 7;
   public const int MMSYSERR_NOTENABLED = 3;
   public const int MMSYSERR_NOTSUPPORTED = 8;
   public const int MMSYSERR_READERROR = 16;
   public const int MMSYSERR_VALNOTFOUND = 19;
   public const int MMSYSERR_WRITEERROR = 17;

   public MIDIExceptions(int errorCode)
   {
      // Get error message.
      StringBuilder errMsg = new StringBuilder(128);
      midiInGetErrorText(errorCode, errMsg, errMsg.Capacity);
   }
}

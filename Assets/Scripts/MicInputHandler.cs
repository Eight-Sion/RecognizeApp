using UnityEngine;
using System.Collections;
using UnityEngine.Audio;
using Assets.Scripts.Model;
using UnityEngine.Events;
using System.Threading.Tasks;

/// <summary>
/// マイク入力を受け取るクラス.
/// 前のバージョンでは,AudioSource をキャッシュしてましたが,
/// Unityの audio 等の便利関数内部では勝手にキャッシュしてくれてるのを
/// 最近知ったので削除しました.
/// 
/// 使い方.
/// 適当な空の GameObject に貼り付けるだけ.
///
/// Microphone -> 現在接続されているマイクを用いて AudioClip へ録音をするクラス.
/// Web Player でこのクラスを使用する場合,
/// Application.RequestUserAuthorization でユーザーの許可を取る必要があるので注意
/// 
/// </summary>

[RequireComponent(typeof(AudioSource))]	//AudioSourceは必須.
[DisallowMultipleComponent]		//複数アタッチさせない.
public class MicInputHandler : MonoBehaviour
{
    //外部から現在の音量を読み取る.
    //最大値は sensitivity による.
    public float GetLoudness()
    {
        return loudness;
    }
    public float sensitivity = 100;     //感度.音量の最大値.
    float loudness;             //音量.
    float lastLoudness;         //前フレームの音量.
    [Range(0, 0.95f)]           //最大1にできてしまうと全く変動しなくなる.
    public float lastLoudnessInfluence; //前フレームの影響度合い.
    void Start() => InitRecord();
    #region Inspector
    public AudioSource audio;
    [SerializeField]
    public IMicInputAction notifyInput;
    public float triggerValue = 10.0f;
    public float intervalForSpeeching = 2.0f;
    #endregion
    /// <summary>
    /// マイク入力による録音を初期化をします.
    /// 
    /// Microphone.devices で現在接続されているマイクを検索し,それを用いて Start()で録音を開始します.
    /// しかし,指定しなくてもデフォルトのマイクを Unity が勝手に選んでくれるので普通に録音できます.便利です.
    ///
    /// Start(string deviceName, bool loop, int lengthSec, int frequency);
    /// 
    /// deviceName 	-> マイク名.指定していない場合,勝手に選ばれるし,柔軟に切り替わります.勝手に切り替わると困る方は指定した方が良いです.
    /// loop 	-> ループさせるか.させないと1回の lengthSec で録音が終了します.
    /// lengthSec	-> 録音して吐き出す AudioClip の長さ.
    /// frequency 	-> 録音して吐き出す AudioClip のサンプルレート
    /// 
    /// </summary>
    void InitRecord()
    {
        //AudioSource の AudioClip を出力先にして,録音開始.
        //マイクで取り扱えるサンプルレートを調べて当てはめることもできますが,今回は一般的な 44100(44.1kHz) を指定しています.
        audio.clip = Microphone.Start(null, true, 10, 44100);
        //録音したデータを延々と取得するためにループさせます.
        audio.loop = true;
        //録音したデータは再生する必要がないのでミュートにします.
        audio.mute = false;
        //録音が開始されるまで待ちます.
        while (!(Microphone.GetPosition("") > 0)) { }
        //データの中身を取得するために再生を始めます.
        audio.Play();
    }
    void LateUpdate() => CalcLoudness();
    private bool isSpeeching = false;
    //現フレームの音量を計算します.
    //マイクの感度が良すぎる場合は, lastLoudnessInfluence を上げたりして調節しましょう.
    void CalcLoudness()
    {
        lastLoudness = loudness;
        loudness = GetAveragedVolume() * sensitivity * (1 - lastLoudnessInfluence) + lastLoudness * lastLoudnessInfluence;
        //Debug.Log(loudness);
        if (!isSpeeching)
        {
            OnSpeeching(
            () => {
                isSpeeching = true;
                notifyInput.StartSpeech();
                StartCoroutine(SpeechingCoroutine());
            });
        }
    }
    IEnumerator SpeechingCoroutine()
    {
        yield return new WaitForSecondsRealtime(intervalForSpeeching);
        OnSpeeching(
            () => {
                StartCoroutine(SpeechingCoroutine());
            }, 
            () => {
                notifyInput.StopSpeech();
                isSpeeching = false;
                StopCoroutine(SpeechingCoroutine());
                InitRecord();
            });
        yield return null;
    }
    void OnSpeeching(UnityAction action, UnityAction error = null)
        => ((loudness > triggerValue) ? action : error)?.Invoke();
    //現フレームで再生されている AudioClip から平均的な音量を取得します.
    float GetAveragedVolume()
    {
        //AudioClip の情報を格納する配列.
        //256は適当です.少なすぎれば平均的なサンプルデータが得られなくなるかもしれず,
        //多すぎれば計算量が増えますので良い感じに...
        float[] data = new float[256];
        //最終的に返す音量データ.
        float a = 0;
        //AudioClipからデータを抽出します.
        audio.GetOutputData(data, 0);
        //音データを絶対値に変換します.
        foreach (float s in data)
        {
            a += Mathf.Abs(s);
        }
        //平均を返します.
        return a / 256;
    }
}
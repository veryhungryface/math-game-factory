using UnityEngine;
using Mgf;
namespace Mgf.HyeopgokSasu
{
    public partial class HyeopgokGame
    {
        Transform kingSelectionRing;
        void BuildKingRing()
        {
            var mesh=new DecorationMesh();
            for(int i=0;i<48;i++){
                float a=i*Mathf.PI*2/48,b=(i+1)*Mathf.PI*2/48;
                mesh.Ribbon(new Vector3(Mathf.Cos(a)*.44f,0,Mathf.Sin(a)*.44f),new Vector3(Mathf.Cos(b)*.44f,0,Mathf.Sin(b)*.44f),.052f,Color.white);
            }
            kingSelectionRing=CreateDecoration("King azure selection ring",mesh,MgfLook.Unlit(MgfLook.Hex("#68e6ff")),false).transform;
            kingSelectionRing.SetParent(null,true);AnimateKingRing();
        }
        void AnimateKingRing()
        {
            if(!kingSelectionRing||!king)return;
            kingSelectionRing.position=new Vector3(king.position.x,1.222f,king.position.z);
            kingSelectionRing.localScale=Vector3.one*(1+Mathf.Sin(Time.unscaledTime*3.6f)*.065f);
        }
    }
}

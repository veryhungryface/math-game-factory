using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Mgf.HyeopgokSasu
{
    [Serializable] public sealed class PackIndex { public string default_pack; public PackEntry[] packs; }
    [Serializable] public sealed class PackEntry { public string pack_id, title, file, school; public int grade, semester, unit_order; }
    [Serializable] public sealed class PackEconomy { public int carry_capacity,coin_per_kill,min_spawn_coins; }
    [Serializable] public sealed class QuestionPack {
        public string pack_id,title,school,unit_id;
        public int schema_version,grade,semester,unit_order;
        public bool has_economy;
        public string[] standards;
        public PackEconomy economy;
        public PackItem[] items;
    }
    [Serializable] public sealed class FractionAnswer { public int num,den; }
    [Serializable] public sealed class PackItem {
        public string id,prompt,answer,format,explain,unitConcept,answer_mode,accept,num_label,den_label,answer_type;
        public string[] choices,distractor_tags;
        public double answerNumeric;
        public int difficulty,max,answerValue,coin_budget;
        public bool has_coin_budget;
        public FractionAnswer answerParts;
        public string Mode=>string.IsNullOrEmpty(answer_mode)?"choice":answer_mode;
        public int Max=>max>0?max:60;
        public string AnswerToken {
            get {
                if(Mode=="amount")return answerValue.ToString();
                if(Mode=="fraction_parts"&&answerParts!=null){int g=Gcd(answerParts.num,answerParts.den);return "{frac:"+(answerParts.num/g)+"/"+(answerParts.den/g)+"}";}
                return answer??"";
            }
        }
        public string PartsToken=>Mode=="fraction_parts"&&answerParts!=null?"{frac:"+answerParts.num+"/"+answerParts.den+"}":AnswerToken;
        // Samples, feedback and the parsed runtime answer must describe the same
        // value that Accepts() expects. Exact-parts fixtures expose their raw pair;
        // every other fraction exposes the canonical reduced token.
        public string PublicAnswerToken=>Mode=="fraction_parts"&&accept=="exact_parts"?PartsToken:AnswerToken;
        public static int Gcd(int a,int b){a=Math.Abs(a);b=Math.Abs(b);while(b!=0){int t=a%b;a=b;b=t;}return Math.Max(1,a);}
        static long Gcd(long a,long b){a=Math.Abs(a);b=Math.Abs(b);while(b!=0){long t=a%b;a=b;b=t;}return Math.Max(1,a);}
        static bool TryChoiceRational(string text,out long numerator,out long denominator){
            numerator=0;denominator=1;if(string.IsNullOrEmpty(text))return false;
            string s=text.Trim();
            if(s.StartsWith("{frac:")&&s.EndsWith("}")){
                string[] parts=s.Substring(6,s.Length-7).Split('/');
                if(parts.Length!=2||!long.TryParse(parts[0],out numerator)||!long.TryParse(parts[1],out denominator)||denominator==0)return false;
            }else if(!long.TryParse(s,out numerator))return false;
            if(denominator<0){numerator=-numerator;denominator=-denominator;}
            long g=Gcd(numerator,denominator);numerator/=g;denominator/=g;return true;
        }
        // Loader validation, scoring and answer highlighting all use this one
        // integer-only policy. Text choices remain exact; rational tokens compare
        // after reduction without ever converting to float/double.
        public static bool EquivalentChoice(string a,string b){
            if(TryChoiceRational(a,out long an,out long ad)&&TryChoiceRational(b,out long bn,out long bd))return an==bn&&ad==bd;
            return a==b;
        }
        public bool Accepts(int denOrAmount,int num){
            if(Mode=="amount")return denOrAmount==answerValue;
            if(Mode!="fraction_parts"||answerParts==null||denOrAmount<=0||num<0)return false;
            if(accept=="exact_parts"||string.IsNullOrEmpty(accept))return num==answerParts.num&&denOrAmount==answerParts.den;
            bool same=(long)num*answerParts.den==(long)answerParts.num*denOrAmount;
            return same&&(accept!="reduced"||Gcd(num,denOrAmount)==1);
        }
    }
    public static class HyeopgokPackJson {
        // JsonUtility has no union fields. Rename only a JSON property named answer,
        // outside quoted strings; preserve every prompt, escape and v1 string verbatim.
        public static QuestionPack Parse(string json){
            var normalized=new StringBuilder(json.Length+128);
            for(int i=0;i<json.Length;){
                if(json[i]!='"'){normalized.Append(json[i++]);continue;}
                int start=i++;
                while(i<json.Length){if(json[i]=='\\'){i+=2;continue;}if(json[i++]=='"')break;}
                int end=i,j=i;while(j<json.Length&&char.IsWhiteSpace(json[j]))j++;
                if(json.Substring(start,end-start)=="\"answer\""&&j<json.Length&&json[j]==':'){
                    int value=j+1;while(value<json.Length&&char.IsWhiteSpace(json[value]))value++;
                    string key=value<json.Length&&json[value]=='{'?"\"answerParts\"":value<json.Length&&(json[value]=='-'||char.IsDigit(json[value]))?"\"answerValue\"":"\"answer\"";
                    if(value>=json.Length)throw new FormatException("Missing answer");
                    string type=json[value]=='{'?"fraction_parts":json[value]=='\"'?"choice":"amount";
                    if(type=="fraction_parts"){
                        int tail=json.IndexOf('}',value);if(tail<0)throw new FormatException("Missing fraction parts");
                        var parts=Regex.Match(json.Substring(value,tail-value+1),@"^\{\s*""(num|den)""\s*:\s*(-?\d+)\s*,\s*""(num|den)""\s*:\s*(-?\d+)\s*\}$");
                        if(!parts.Success||parts.Groups[1].Value==parts.Groups[3].Value||!int.TryParse(parts.Groups[2].Value,out _)||!int.TryParse(parts.Groups[4].Value,out _))throw new FormatException("Both fraction parts must be integers");
                    }
                    if(type=="amount"){
                        int tail=value;while(tail<json.Length&&json[tail]!=','&&json[tail]!='}')tail++;
                        if(!int.TryParse(json.Substring(value,tail-value).Trim(),out _))throw new FormatException("Answer must be an integer");
                    }
                    normalized.Append("\"answer_type\":\"").Append(type).Append("\",").Append(key);
                }else {
                    string key=json.Substring(start,end-start);
                    if(j<json.Length&&json[j]==':'&&key=="\"economy\"")normalized.Append("\"has_economy\":true,");
                    if(j<json.Length&&json[j]==':'&&(key=="\"coin_budget\""||key=="\"max\""||key=="\"carry_capacity\""||key=="\"coin_per_kill\""||key=="\"min_spawn_coins\""||key=="\"schema_version\"")){
                        int value=j+1,tail=value;while(tail<json.Length&&json[tail]!=','&&json[tail]!='}')tail++;
                        if(!int.TryParse(json.Substring(value,tail-value).Trim(),out _))throw new FormatException("Economy and input limits must be integers");
                        if(key=="\"coin_budget\"")normalized.Append("\"has_coin_budget\":true,");
                    }
                    normalized.Append(json,start,end-start);
                }
            }
            var pack=JsonUtility.FromJson<QuestionPack>(normalized.ToString());
            // Unity may instantiate missing inline serializable objects with zero
            // fields. Preserve actual JSON absence so v1 needs no economy profile,
            // while schema v2 still fails its required-profile validation.
            if(pack!=null&&!pack.has_economy)pack.economy=null;
            if(pack!=null&&pack.items!=null)foreach(var q in pack.items){
                if(q==null)continue;
                if(q.Mode=="amount"){q.answer=q.AnswerToken;q.answerNumeric=q.answerValue;q.format="int";}
                else if(q.Mode=="fraction_parts"&&q.answerParts!=null){q.answer=q.PublicAnswerToken;q.answerNumeric=q.answerParts.den==0?double.NaN:(double)q.answerParts.num/q.answerParts.den;q.format="frac";}
            }
            return pack;
        }
    }
}

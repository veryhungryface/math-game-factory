// 화면 캡처만 읽는 평가자의 눈: macOS Vision OCR + 세로 자 눈금 색상 검출.
import Foundation
import Vision
import AppKit

let mode=CommandLine.arguments[1]
let path=CommandLine.arguments[2]
guard let image=NSImage(contentsOfFile:path),let cg=image.cgImage(forProposedRect:nil,context:nil,hints:nil) else {print("null");exit(1)}
let width=cg.width,height=cg.height
func emit(_ value:Any){print(String(data:try! JSONSerialization.data(withJSONObject:value),encoding:.utf8)!)}

if mode=="ocr"{
    let request=VNRecognizeTextRequest();request.recognitionLevel = .accurate;request.recognitionLanguages=["ko-KR","en-US"];request.usesLanguageCorrection=false
    try? VNImageRequestHandler(cgImage:cg,options:[:]).perform([request])
    var out:[[String:Any]]=[]
    for observation in request.results ?? []{guard let top=observation.topCandidates(1).first else{continue};let b=observation.boundingBox;out.append(["text":top.string,"x":b.minX*Double(width),"y":(1-b.maxY)*Double(height),"w":b.width*Double(width),"h":b.height*Double(height)])}
    emit(out);exit(0)
}

var pixels=[UInt8](repeating:0,count:width*height*4)
let context=CGContext(data:&pixels,width:width,height:height,bitsPerComponent:8,bytesPerRow:width*4,space:CGColorSpaceCreateDeviceRGB(),bitmapInfo:CGImageAlphaInfo.premultipliedLast.rawValue)!
context.draw(cg,in:CGRect(x:0,y:0,width:width,height:height))
if mode=="ruler"{
    var minX=width,minY=height,maxX=0,maxY=0,count=0
    for y in Int(Double(height)*0.52)..<height{
        for x in Int(Double(width)*0.69)..<Int(Double(width)*0.96){
            let i=(y*width+x)*4,r=Int(pixels[i]),g=Int(pixels[i+1]),b=Int(pixels[i+2])
            if r>=78 && r<=145 && g>=24 && g<=75 && b>=32 && b<=92 && r>g+30{
                minX=min(minX,x);maxX=max(maxX,x);minY=min(minY,y);maxY=max(maxY,y);count+=1
            }
        }
    }
    if count<500{emit(["count":count]);exit(2)}
    emit(["x":minX,"y":minY,"w":maxX-minX+1,"h":maxY-minY+1,"count":count]);exit(0)
}
emit(["error":"unknown mode"]);exit(2)

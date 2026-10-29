// 화면 캡처만 읽는 평가자의 눈. Unity 상태·문제은행·정답 훅을 보지 않는다.
import Foundation
import Vision
import AppKit

guard CommandLine.arguments.count == 2,
      let image = NSImage(contentsOfFile: CommandLine.arguments[1]),
      let cg = image.cgImage(forProposedRect: nil, context: nil, hints: nil) else {
    fputs("usage: screen-ocr <png>\n", stderr)
    exit(2)
}

let request = VNRecognizeTextRequest()
request.recognitionLevel = .accurate
request.recognitionLanguages = ["ko-KR", "en-US"]
request.usesLanguageCorrection = false
try? VNImageRequestHandler(cgImage: cg, options: [:]).perform([request])
let width = cg.width, height = cg.height
let rows: [[String: Any]] = (request.results ?? []).compactMap { observation in
    guard let top = observation.topCandidates(1).first else { return nil }
    let b = observation.boundingBox
    return [
        "text": top.string,
        "x": b.minX * Double(width),
        "y": (1 - b.maxY) * Double(height),
        "w": b.width * Double(width),
        "h": b.height * Double(height)
    ]
}
let data = try! JSONSerialization.data(withJSONObject: rows)
print(String(data: data, encoding: .utf8)!)

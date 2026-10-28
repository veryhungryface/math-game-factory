import AppKit
import Foundation
import Vision

guard CommandLine.arguments.count == 2 else {
    fputs("usage: screen-ocr <png>\n", stderr)
    exit(2)
}
let url = URL(fileURLWithPath: CommandLine.arguments[1])
guard let image = NSImage(contentsOf: url),
      let data = image.tiffRepresentation,
      let bitmap = NSBitmapImageRep(data: data),
      let cg = bitmap.cgImage else {
    fputs("cannot read image\n", stderr)
    exit(3)
}

let accurate = VNRecognizeTextRequest()
accurate.recognitionLevel = .accurate
accurate.recognitionLanguages = ["ko-KR", "en-US"]
accurate.usesLanguageCorrection = false
accurate.customWords = (1...24).map(String.init) + ["현상", "첫 제출", "새 건판", "시작", "건판을", "빛에 대기"]
accurate.minimumTextHeight = 0.018
let fast = VNRecognizeTextRequest()
fast.recognitionLevel = .fast
fast.recognitionLanguages = ["en-US"]
fast.usesLanguageCorrection = false
fast.minimumTextHeight = 0.018
try VNImageRequestHandler(cgImage: cg, options: [:]).perform([accurate, fast])

let observations = (accurate.results ?? []) + (fast.results ?? [])
let rows: [[String: Any]] = observations.compactMap { observation in
    guard let top = observation.topCandidates(1).first else { return nil }
    let b = observation.boundingBox
    return [
        "text": top.string,
        "confidence": top.confidence,
        "x": b.origin.x,
        "y": b.origin.y,
        "w": b.size.width,
        "h": b.size.height,
    ]
}
let out = try JSONSerialization.data(withJSONObject: rows, options: [.prettyPrinted, .sortedKeys])
FileHandle.standardOutput.write(out)
FileHandle.standardOutput.write(Data("\n".utf8))

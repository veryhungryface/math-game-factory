Three.js r185.1 glTF loader dependencies.
BufferGeometryUtils.js and SkeletonUtils.js are unchanged copies from the upstream three npm
package r185.1 (same files as orbit-courier / twin-forts already use). MIT license in LICENSE.

왜 필요한가: public/vendor/addons/loaders/GLTFLoader.js 는 `../utils/BufferGeometryUtils.js`
와 `../utils/SkeletonUtils.js` 를 import 하는데 vendor 에는 utils/ 폴더가 없다. 그대로 두면
404 두 건이 나고 모듈 로드가 실패한다. 게임의 import map 이 그 두 상대 경로를
이 폴더로 되돌린다. vendor 파일은 편집하지 않았다.

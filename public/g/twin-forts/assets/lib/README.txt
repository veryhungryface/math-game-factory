Three.js r185.1 glTF loader dependencies.
BufferGeometryUtils.js and SkeletonUtils.js are unchanged copies from the upstream three npm package r185.1, already used by this repository's orbit-courier integration. Original MIT license included as LICENSE.
These local files satisfy the missing relative utility imports of ../../vendor/addons/loaders/GLTFLoader.js via the game's import map. No vendor files were edited.

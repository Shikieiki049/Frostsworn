// Parse and sample the actual Spine runtime, without a browser or GPU.
const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const path = require('node:path');
const root = __dirname;
const context = vm.createContext({console, Float32Array, Uint16Array, Uint32Array, Int16Array, Uint8Array, Math});
vm.runInContext(fs.readFileSync(path.join(root, 'spine-webgl.js'), 'utf8'), context);
const spine = context.spine;
const atlas = new spine.TextureAtlas(fs.readFileSync(path.join(root, 'frostsworn-idle.atlas'), 'utf8'));
for (const page of atlas.pages) page.setTexture({getImage:()=>({width:1254,height:1254}),setFilters(){},setWraps(){}});
const source = JSON.parse(fs.readFileSync(path.join(root, 'frostsworn-idle.json'), 'utf8'));
const data = new spine.SkeletonJson(new spine.AtlasAttachmentLoader(atlas)).readSkeletonData(source);
assert.equal(data.animations.length, 1);
assert.equal(data.animations[0].name, 'idle_loop');
assert.equal(data.animations[0].duration, 6);
const skeleton = new spine.Skeleton(data);
const state = new spine.AnimationState(new spine.AnimationStateData(data));
state.setAnimation(0, 'idle_loop', true);
function sample(t) {
  skeleton.setToSetupPose();
  state.getCurrent(0).trackTime = t;
  state.apply(skeleton);
  skeleton.updateWorldTransform(spine.Physics.none);
  const slot = skeleton.findSlot('character');
  const result = new Float32Array(slot.attachment.worldVerticesLength);
  slot.attachment.computeWorldVertices(slot, 0, result.length, result, 0, 2);
  assert.ok(Array.from(result).every(Number.isFinite));
  return result;
}
const start=sample(0), middle=sample(3), end=sample(6);
const uvs=source.skins[0].attachments.character.body.uvs;
let movement=0, footMovement=0, seam=0;
for(let i=0;i<start.length;i+=2){
  movement=Math.max(movement,Math.hypot(start[i]-middle[i],start[i+1]-middle[i+1]));
  seam=Math.max(seam,Math.hypot(start[i]-end[i],start[i+1]-end[i+1]));
  if(uvs[i+1]*1254>1100) footMovement=Math.max(footMovement,Math.hypot(start[i]-middle[i],start[i+1]-middle[i+1]));
}
assert.ok(movement>1, 'Breathing and secondary motion should change the mesh.');
assert.ok(footMovement<0.001, 'Feet must stay planted.');
assert.ok(seam<0.001, 'Loop endpoints must match.');
sample(2.45); assert.equal(skeleton.findSlot('eyes').attachment.name, 'blink');
sample(2.60); assert.equal(skeleton.findSlot('eyes').attachment, null);
console.log(JSON.stringify({duration:6,bones:data.bones.length,vertices:start.length/2,maxMotion:movement,footMotion:footMovement,loopSeam:seam,blink:'passed'}));

// Sample the approved first idle with the official Spine runtime. No raster edits.
const fs = require('node:fs'), path = require('node:path'), vm = require('node:vm');
const source = path.resolve(__dirname, '../AnimationPreview/idle-v1');
const output = path.resolve(__dirname, '../Assets/Frostsworn/idle');
fs.mkdirSync(output, {recursive:true});
const ctx = vm.createContext({console, Float32Array, Uint16Array, Uint32Array, Int16Array, Uint8Array, Math});
vm.runInContext(fs.readFileSync(path.join(source,'spine-webgl.js'),'utf8'),ctx);
const spine=ctx.spine;
const atlas=new spine.TextureAtlas(fs.readFileSync(path.join(source,'frostsworn-idle.atlas'),'utf8'));
for(const page of atlas.pages) page.setTexture({getImage:()=>({width:1254,height:1254}),setFilters(){},setWraps(){}});
const json=JSON.parse(fs.readFileSync(path.join(source,'frostsworn-idle.json'),'utf8'));
const data=new spine.SkeletonJson(new spine.AtlasAttachmentLoader(atlas)).readSkeletonData(json);
const skeleton=new spine.Skeleton(data), state=new spine.AnimationState(new spine.AnimationStateData(data));
state.setAnimation(0,'idle_loop',true);
const meshes=[json.skins[0].attachments.character.body,json.skins[0].attachments.eyes.blink];
const count=180, stride=meshes.reduce((n,m)=>n+m.uvs.length,0);
const binary=Buffer.alloc(count*stride*4), blink=[];
for(let f=0;f<count;f++){
  skeleton.setToSetupPose(); state.getCurrent(0).trackTime=f/30; state.apply(skeleton); skeleton.updateWorldTransform(spine.Physics.none);
  blink.push(skeleton.findSlot('eyes').attachment!==null);
  let offset=f*stride*4;
  for(const [slotName,attachmentName] of [['character','body'],['eyes','blink']]){
    const slot=skeleton.findSlot(slotName), attachment=skeleton.getAttachment(slot.data.index,attachmentName);
    const vertices=new Float32Array(attachment.worldVerticesLength);
    attachment.computeWorldVertices(slot,0,vertices.length,vertices,0,2);
    for(let i=0;i<vertices.length;i++) {binary.writeFloatLE(i%2 ? -vertices[i] : vertices[i],offset);offset+=4;}
  }
}
fs.writeFileSync(path.join(output,'vertices.bin'),binary);
fs.writeFileSync(path.join(output,'mesh.json'),JSON.stringify({fps:30,count,stride,blink,meshes:meshes.map(m=>({uvs:m.uvs.map(v=>v*1254),triangles:m.triangles}))}));
for(const n of ['full','full-blink']) fs.copyFileSync(path.join(source,'assets',n+'.png'),path.join(output,n+'.png'));
console.log(`Exported first idle: ${count} frames, ${binary.length} bytes of mesh data.`);

const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict'),path=require('node:path');
const root=__dirname,env=vm.createContext({console,Float32Array,Uint16Array,Uint32Array,Int16Array,Uint8Array,Math});
vm.runInContext(fs.readFileSync(path.join(root,'spine-webgl.js'),'utf8'),env);const spine=env.spine;
const json=JSON.parse(fs.readFileSync(path.join(root,'frostsworn-attacks.json'),'utf8'));
const atlas=new spine.TextureAtlas(fs.readFileSync(path.join(root,'frostsworn-attacks.atlas'),'utf8'));
for(const page of atlas.pages)page.setTexture({getImage:()=>({width:page.width,height:page.height}),setFilters(){},setWraps(){}});
const data=new spine.SkeletonJson(new spine.AtlasAttachmentLoader(atlas)).readSkeletonData(json);
assert.equal(data.ikConstraints.length,2);
const parts=json.skins[0].attachments;
for(const side of ['free','staff'])for(const limb of ['upper','forearm']){
 const name=limb+'-'+side;
 assert.equal(parts[name][name].path,'sleeve-'+side+'-v2','Arm clothing must use the sleeve-only texture, never the source containing a detached hand.');
}
assert.equal(parts.torso.torso.path,'torso-clean-v2');
assert.equal(parts.torso.torso.vertices.length,8,'Continuous body texture must not be split into cropped garment fragments.');

const order=json.slots.map(s=>s.name);
assert.ok(!order.includes('robe'),'Chest, belt and robe must be one uninterrupted attachment.');
assert.ok(order.indexOf('staff')>order.indexOf('forearm-staff'));
assert.ok(order.indexOf('staff')>order.indexOf('torso'));
assert.ok(order.indexOf('hand-staff')>order.indexOf('staff'),'Gripping fingers must cover the shaft.');
for(const group of Object.values(json.skins[0].attachments))for(const mesh of Object.values(group))assert.equal(mesh.vertices.length,mesh.uvs.length,'Attachments must stay rigid.');
const model=new spine.Skeleton(data),state=new spine.AnimationState(new spine.AnimationStateData(data));
function pose(name,t){model.setToSetupPose();if(!state.getCurrent(0)||state.getCurrent(0).animation.name!==name)state.setAnimation(0,name,false);state.getCurrent(0).trackTime=t;state.apply(model);model.updateWorldTransform(spine.Physics.none);const result={};for(const slot of model.slots){const mesh=slot.attachment;if(!mesh)continue;const v=new Float32Array(mesh.worldVerticesLength);mesh.computeWorldVertices(slot,0,v.length,v,0,2);assert.ok(Array.from(v).every(Number.isFinite));result[slot.data.name]=v;}return result;}
let totalFrames=0,maxStretch=0,maxAnkleError=0;
for(const name of ['attack_magic','attack_melee']){
 const duration=data.findAnimation(name).duration,start=pose(name,0);let maxMove=0,seam=0;
 for(let f=0;f<=Math.round(duration*60);f++){
  const t=f/60,current=pose(name,t);totalFrames++;
  const wrist=model.findBone('wrist_staff'),staff=model.findBone('staff');
  const grip=wrist.localToWorld(new spine.Vector2(43,-14));
  assert.ok(Math.hypot(grip.x-staff.worldX,grip.y-staff.worldY)<.001,'Staff grip must stay inside the hand.');
  for(const [n,v] of Object.entries(start)){
   const w=current[n];assert.ok(w);
   for(let i=0;i<v.length;i+=2){const moved=Math.hypot(v[i]-w[i],v[i+1]-w[i+1]);maxMove=Math.max(maxMove,moved);if(n.startsWith('shoe-'))assert.ok(moved<.001,'Feet must stay planted.');if(f===Math.round(duration*60))seam=Math.max(seam,moved);for(let j=i+2;j<v.length;j+=2)maxStretch=Math.max(maxStretch,Math.abs(Math.hypot(v[i]-v[j],v[i+1]-v[j+1])-Math.hypot(w[i]-w[j],w[i+1]-w[j+1])));}
  }
  for(const side of ['left','right']){const shin=model.findBone('shin_'+side),foot=model.findBone('foot_'+side),ankle=shin.localToWorld(new spine.Vector2(shin.data.length,0));maxAnkleError=Math.max(maxAnkleError,Math.hypot(ankle.x-foot.worldX,ankle.y-foot.worldY));}
 }
 assert.ok(seam<.002,'Attack must return to the identical starting pose.');assert.ok(maxMove>100,'Attack must have a readable range of motion.');console.log(JSON.stringify({animation:name,duration,maxMotion:maxMove,returnPoseError:seam}));
}
assert.ok(maxStretch<.002,'Faces, hands, fabric motifs and staff must not stretch.');
console.log(JSON.stringify({framesChecked:totalFrames,maxDetailStretch:maxStretch,maxAnkleError,bones:data.bones.length,parts:json.slots.length}));
for(const [name,t] of [['attack_magic',1.05],['attack_melee',.74]]){pose(name,t);const b=model.findBone('staff');console.log(name+' staff origin '+JSON.stringify({x:b.worldX,y:b.worldY}));}

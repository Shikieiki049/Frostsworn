"""Validate selected FMOD events against the installed game; never bundle banks."""
import argparse
import ctypes as c
import json
import os
from pathlib import Path
import re
import struct
import tempfile

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--game-path', required=True)
    parser.add_argument('--output', default='Tests/native-audio-0826.json')
    args = parser.parse_args()
    root = Path(__file__).resolve().parent.parent
    game = Path(args.game_path)
    events = re.findall(r'public const string \w+="([^"]+)"', (root/'Source/CardAudio.cs').read_text(encoding='utf-8'))
    banks = {'banks/desktop/'+name for name in ['Master.strings.bank', 'Master.bank', 'sfx.bank']}
    with tempfile.TemporaryDirectory(prefix='frost-audio-check-') as temp:
        with (game/'SlayTheSpire2.pck').open('rb') as pack:
            header = pack.read(40)
            if header[:4] != b'GDPC' or struct.unpack_from('<I',header,4)[0] != 3:
                raise RuntimeError('Expected the supported game PCK version 3')
            base, index = struct.unpack_from('<QQ',header,24)
            pack.seek(index)
            entries = {}
            for _ in range(struct.unpack('<I',pack.read(4))[0]):
                name = pack.read(struct.unpack('<I',pack.read(4))[0]).decode().rstrip('\0')
                offset, size = struct.unpack('<QQ',pack.read(16))
                pack.read(16)
                flags = struct.unpack('<I',pack.read(4))[0]
                if name in banks:
                    if flags: raise RuntimeError('Unsupported protected bank entry')
                    entries[name] = (base+offset,size)
            for name in banks:
                offset,size = entries[name]
                pack.seek(offset)
                (Path(temp)/Path(name).name).write_bytes(pack.read(size))
        with os.add_dll_directory(str(game.resolve())):
            studio = c.CDLL(str((game/'fmodstudio.dll').resolve()))
            core = c.CDLL(str((game/'fmod.dll').resolve()))
            P,I,U = c.c_void_p,c.c_int,c.c_uint
            def call(lib,name,types,*values):
                method = getattr(lib,name);method.argtypes = types;method.restype = I
                result = method(*values)
                if result: raise RuntimeError(f'{name} failed: FMOD result {result}')
            system = P()
            call(studio,'FMOD_Studio_System_Create',[c.POINTER(P),U],c.byref(system),0x00020306)
            try:
                mixer = P()
                call(studio,'FMOD_Studio_System_GetCoreSystem',[P,c.POINTER(P)],system,c.byref(mixer))
                call(core,'FMOD_System_SetOutput',[P,I],mixer,2) # NOSOUND, no hardware playback.
                call(studio,'FMOD_Studio_System_Initialize',[P,I,U,U,P],system,32,4,0,None)
                for name in ['Master.strings.bank','Master.bank','sfx.bank']:
                    bank = P()
                    call(studio,'FMOD_Studio_System_LoadBankFile',[P,c.c_char_p,U,c.POINTER(P)],system,str(Path(temp)/name).encode('utf-8'),0,c.byref(bank))
                report = {}
                for event in events:
                    description = P();one = I()
                    call(studio,'FMOD_Studio_System_GetEvent',[P,c.c_char_p,c.POINTER(P)],system,event.encode(),c.byref(description))
                    call(studio,'FMOD_Studio_EventDescription_IsOneshot',[P,c.POINTER(I)],description,c.byref(one))
                    if not one.value: raise RuntimeError('Looping sound rejected: '+event)
                    report[event] = {'oneshot':True}
                output = root/args.output
                output.parent.mkdir(parents=True,exist_ok=True)
                output.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
                print(f'Validated {len(report)} native one-shot events; report: {output}')
            finally:
                call(studio,'FMOD_Studio_System_Release',[P],system)

if __name__ == '__main__':
    main()

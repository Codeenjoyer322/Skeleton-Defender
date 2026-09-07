"""Copy finished Testnew2 PNG atlases into Unity without modifying any artwork.

Metadata normalization and alpha-bound measurements are read-only. Runtime aliases
share atlas resources. Re-running preserves stable Unity GUIDs and verifies hashes.
"""
from pathlib import Path
import argparse, copy, hashlib, json, math, shutil, uuid
from collections import defaultdict
from PIL import Image

REPO = Path(__file__).resolve().parents[1]
DEST = REPO / 'Assets/Resources/AnimationPack'
GUID_NAMESPACE = uuid.UUID('bc4db2a3-365e-4bdb-a390-8c24d57d7c68')


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_json(path, obj):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(obj, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def meta_file(path, folder=False):
    target = Path(str(path) + '.meta')
    if target.exists():
        return
    guid = uuid.uuid5(GUID_NAMESPACE, path.relative_to(REPO).as_posix()).hex
    text = f'fileFormatVersion: 2\nguid: {guid}\n'
    if folder:
        text += 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    target.write_text(text, encoding='utf-8')


def point(p, fallback=None):
    if isinstance(p, dict) and isinstance(p.get('x'), (int, float)) and isinstance(p.get('y'), (int, float)):
        return {'x': float(p['x']), 'y': float(p['y'])}
    return copy.deepcopy(fallback)


def mirrored(p, width):
    return {'x': width - 1 - p['x'], 'y': p['y']}


def collect(root):
    catalog = read(root / 'source/review_catalog.json')
    entries = [(e, 'source/review_catalog.json') for e in catalog['bootstrap']]
    manifests = set()
    for approval_root in catalog['approvalRoots']:
        for name in ('delivery-approved.json', 'delivery_manifest.json'):
            manifests.update((root / approval_root).rglob(name))
    for path in sorted(manifests):
        data = read(path)
        assert data.get('schemaVersion') == 1 and isinstance(data.get('assets'), list), path
        entries.extend((e, path.relative_to(root).as_posix()) for e in data['assets'])
    # Finished standalone components precede the gallery catalog. Their native
    # metadata explicitly identifies them as gameplay components. Rain_Demo is
    # excluded: its fixed seven-arrow layout is only a presentation example.
    for suffix, action, loop in [('Divine_Spear_Projectile', 'divine_spear_projectile', True),
                                 ('Ordinary_Arrow_Projectile', 'ordinary_arrow_projectile', True),
                                 ('Blue_Heel_Arrow_Rise_Burst', 'blue_heel_arrow_rise_burst', False)]:
        path = f'animations/effects/VFX_{suffix}_sheet.json'
        assert read(root / path)['meta']['componentOnly']
        entries.append((dict(id='achilles-fx-' + action, status='approved', category='effects',
                             character='achilles', action=action, direction='none',
                             playback='loop' if loop else 'once', metadata=path), 'native-component-metadata'))
    result, excluded, seen = [], [], set()
    for entry, approval in entries:
        if entry.get('status') != 'approved':
            continue
        assert entry['id'] not in seen, entry['id']
        seen.add(entry['id'])
        if 'demo' in entry['action'].lower() or '/demos/' in entry['metadata'] or '/targeting_demos/' in entry['metadata']:
            excluded.append(entry['id'])
            continue
        path = (root / entry['metadata']).resolve()
        path.relative_to(root.resolve())
        data = read(path)
        sheet = path.parent / data['meta']['image']
        assert sheet.is_file() and sheet.parent == path.parent
        with Image.open(sheet) as image:
            sw, sh = image.size
        assert data['meta']['size'] == {'w': sw, 'h': sh}
        frames = []
        for f in data['frames']:
            rect, canvas, trim = f['frame'], f['sourceSize'], f['spriteSourceSize']
            assert not f.get('rotated')
            assert 0 <= rect['x'] < sw and 0 <= rect['y'] < sh
            assert rect['x'] + rect['w'] <= sw and rect['y'] + rect['h'] <= sh
            assert f['duration'] > 0
            frames.append(dict(x=rect['x'], y=rect['y'], w=rect['w'], h=rect['h'],
                               sourceX=trim['x'], sourceY=trim['y'], durationSeconds=f['duration'] / 1000.0))
        result.append(dict(entry=entry, approval=approval, metadata=data, path=path, sheet=sheet,
                           frames=frames, sheetWidth=sw, sheetHeight=sh,
                           width=data['frames'][0]['sourceSize']['w'], height=data['frames'][0]['sourceSize']['h']))
    return result, excluded


def role_key(item):
    e = item['entry']
    return 'circe_deer' if e['character'] == 'circe' and e['action'] == 'deer_run' else e['character']


def canonical_anchors(items):
    by_role, result = defaultdict(list), {}
    for item in items:
        if item['entry']['category'] != 'effects' or item['entry']['action'] == 'deer_run':
            by_role[role_key(item)].append(item)
    for role, group in by_role.items():
        candidates = sorted(group, key=lambda i: (i['entry']['action'] not in ('idle',),
                                                  i['entry']['action'] not in ('walk', 'deer_run', 'hop'),
                                                  i['entry']['direction'] != 'east'))
        ref = candidates[0]
        frame = ref['metadata']['frames'][0]
        r, trim = frame['frame'], frame['spriteSourceSize']
        with Image.open(ref['sheet']) as image:
            bounds = image.convert('RGBA').crop((r['x'], r['y'], r['x'] + r['w'], r['y'] + r['h'])).getchannel('A').getbbox()
        assert bounds, role
        bounds = [bounds[0]+trim['x'], bounds[1]+trim['y'], bounds[2]+trim['x'], bounds[3]+trim['y']]
        # Prefer the authored shared ground coordinate over alpha measurements:
        # death FX, airborne legs, raised weapons must never move the world pivot.
        anchor = None
        for item in sorted(group, key=lambda i: i['entry']['direction'] != 'east'):
            m = item['metadata']['meta']
            p = point(m.get('pivot')) or point(m.get('groundAnchor')) or point(m.get('anchors', {}).get('groundPivot'))
            if not p and m.get('anchor', {}).get('type') == 'shared_ground_reference':
                p = point(m['anchor'])
            if p:
                anchor = mirrored(p, item['width']) if item['entry']['direction'] == 'west' else p
                break
        evidence = 'authored shared ground anchor; constant across all actions'
        if not anchor:
            anchor = {'x': ref['width'] / 2, 'y': bounds[3] - 1}
            evidence = 'read-only alpha bound of idle/first walk pose; constant across all actions'
        top = bounds[1]
        hit = {'x': anchor['x'], 'y': round(anchor['y'] - (anchor['y'] - top) * .4)}
        if role in ('normal', 'sarcophagus_bare'):
            hit = {'x': 80, 'y': 95}  # Approved targeting sandbox torso contract.
        result[role] = dict(ground=anchor, hit=hit, reference=ref['entry']['id'], bounds=bounds, evidence=evidence)
    return result


def events_and_sockets(item, root, contracts):
    e, m, frames = item['entry'], item['metadata']['meta'], item['frames']
    raw_events = copy.deepcopy(m.get('events') or [])
    sockets = []
    for row in m.get('emitterTrack', []):
        for name in ('staffTip',):
            p = point(row.get(name))
            if p:
                sockets.append(dict(name=name, frameIndex=row['frameIndex'], position=p))
    for name, rows in (m.get('sockets') or {}).items():
        if not isinstance(rows, list):
            continue
        for row in rows:
            p = point(row)
            if p:
                sockets.append(dict(name=name, frameIndex=row.get('frameIndex', 0), position=p))
    if e['character'] == 'achilles' and e['category'] == 'heroes':
        action = 'sword_attack' if e['action'] == 'attack' else e['action']
        contract = contracts.get(action, {})
        if contract:
            raw_events = copy.deepcopy(contract.get('events', []))
            raw_events += copy.deepcopy(contract.get('artCues', []))
            for name, row in contract.get('sockets', {}).items():
                p = point(row.get(e['direction']))
                if p:
                    sockets.append(dict(name=name, frameIndex=row['frameIndex'], position=p))
    normalized = []
    for ev in raw_events:
        name = ev.get('name') or ev.get('event') or 'cue'
        index = ev.get('frameIndex', ev.get('index', ev.get('frame', 0)))
        # Authored exact time wins over inconsistent legacy frame numbering.
        time = ev.get('timeMs')
        if time is not None:
            elapsed, best = 0, 0
            for i, f in enumerate(frames):
                if abs(elapsed - time / 1000) < .00001:
                    best = i
                    break
                elapsed += f['durationSeconds']
            index = best
        else:
            time = sum(f['durationSeconds'] for f in frames[:index]) * 1000
        assert 0 <= index < len(frames)
        p = point(ev.get('position')) or point(ev.get('socket'))
        socket_name = ev.get('socket') if isinstance(ev.get('socket'), str) else ev.get('emitter') or ev.get('actorSocket') or ''
        if not p and socket_name:
            p = next((s['position'] for s in sockets if s['name'] == socket_name and s['frameIndex'] == index), None)
        if not p and name == 'visual_contact':
            p = point(m.get('contactPoint'))
        normalized.append(dict(name=name, frameIndex=index, timeSeconds=round(time / 1000, 6),
                               hasPosition=p is not None, position=p or {'x': 0, 'y': 0}, socketName=socket_name))
    return normalized, sockets


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, default=Path(r'C:\Users\Life\Desktop\Testnew2'))
    args = parser.parse_args()
    root = args.source.resolve()
    items, excluded = collect(root)
    anchors = canonical_anchors(items)
    contracts = {a['action']: a for a in read(root/'animations/achilles_left/achilles_direction_contract.json')['actions']}
    texture_dir = DEST / 'Textures'
    texture_dir.mkdir(parents=True, exist_ok=True)
    textures, clips, provenance = {}, [], []
    for item in items:
        e, m = item['entry'], item['metadata']['meta']
        digest = sha(item['sheet'])
        name = 'atlas_' + digest[:20]
        dest = texture_dir / (name + '.png')
        if digest not in textures:
            if not dest.exists() or sha(dest) != digest:
                shutil.copyfile(item['sheet'], dest)
            assert sha(dest) == digest
            meta_file(dest)
            textures[digest] = dest
        width, height = item['width'], item['height']
        center = {'x': width / 2, 'y': height / 2}
        is_actor = e['category'] != 'effects' or e['action'] == 'deer_run'
        if is_actor:
            a = anchors[role_key(item)]
            ground, hit = copy.deepcopy(a['ground']), copy.deepcopy(a['hit'])
            if e['direction'] == 'west':
                ground, hit = mirrored(ground, width), mirrored(hit, width)
            origin = ground
        else:
            origin = point(m.get('anchor')) or point(m.get('anchors', {}).get('origin')) or center
            ground = hit = origin
        fx = m.get('anchors') or {}
        axis = point(fx.get('rotationAxis'), {'x': 1, 'y': 0})
        if e['action'] == 'blue_heel_arrow_rise_burst':
            axis = {'x': 0, 'y': -1}
        end = point(fx.get('endPoint')) or point(m.get('anchor', {}).get('burst')) or origin
        events, sockets = events_and_sockets(item, root, contracts)
        f0 = item['frames'][0]
        with Image.open(item['sheet']) as image:
            b = image.convert('RGBA').crop((f0['x'], f0['y'], f0['x']+f0['w'], f0['y']+f0['h'])).getchannel('A').getbbox()
        b = b or (0, 0, width, height)
        duration = round(sum(f['durationSeconds'] for f in item['frames']), 6)
        clip = dict(id=e['id'], character=e['character'], action=e['action'], category=e['category'],
                    direction=e['direction'], resourcePath='AnimationPack/Textures/' + name,
                    width=width, height=height, sheetWidth=item['sheetWidth'], sheetHeight=item['sheetHeight'],
                    duration=duration, loop=e['playback'] == 'loop', flipX=False,
                    groundPivot=ground, hitPoint=hit, origin=origin, endPoint=end, rotationAxis=axis,
                    opaqueX=b[0]+f0['sourceX'], opaqueY=b[1]+f0['sourceY'], opaqueWidth=b[2]-b[0], opaqueHeight=b[3]-b[1],
                    displayScale=float(e.get('displayScale', m.get('displayScale', 1))),
                    frames=item['frames'], events=events, sockets=sockets, aliasOf='',
                    sourceSheetSha256=digest, sourceMetadata=e['metadata'])
        clips.append(clip)
        provenance.append(dict(id=e['id'], approvalSource=item['approval'], sourceSheet=item['sheet'].relative_to(root).as_posix(),
                               sourceMetadata=e['metadata'], sourceSheetSha256=digest, sourceMetadataSha256=sha(item['path']),
                               runtimeTexture=dest.relative_to(REPO).as_posix()))
    # Ready-pose aliases are metadata only. There is no invented Achilles hurt
    # animation: the existing ready frame plus the renderer's hit tint is used.
    for direction in ('east', 'west'):
        for role, action, source_action in [('achilles', 'idle', 'attack'), ('achilles', 'hurt', 'attack'), ('giant', 'idle', 'walk')]:
            source = next(c for c in clips if c['character'] == role and c['action'] == source_action and c['direction'] == direction)
            alias = copy.deepcopy(source)
            alias.update(id=f'{role}-{action}-{direction}-ready-alias', action=action, loop=action=='idle',
                         aliasOf=source['id'], events=[], sockets=[], duration=1.0 if action=='idle' else .12)
            alias['frames'] = [copy.deepcopy(source['frames'][0])]
            alias['frames'][0]['durationSeconds'] = alias['duration']
            clips.append(alias)
    # Still references exist in one authored direction. Flip their UVs at draw
    # time for the other direction; do not create duplicate raster resources.
    for source in list(clips):
        if source['action'] != 'idle' or source['direction'] not in ('east', 'west'):
            continue
        other = 'west' if source['direction'] == 'east' else 'east'
        if any(c['character']==source['character'] and c['action']=='idle' and c['direction']==other for c in clips):
            continue
        alias = copy.deepcopy(source)
        alias.update(id=f"{source['character']}-idle-{other}-mirror-alias", direction=other, flipX=True, aliasOf=source['id'])
        alias['opaqueX'] = alias['width'] - alias['opaqueX'] - alias['opaqueWidth']
        for field in ('groundPivot', 'hitPoint', 'origin', 'endPoint'):
            alias[field] = mirrored(alias[field], alias['width'])
        clips.append(alias)
    keys, ids = set(), set()
    for c in clips:
        key = (c['character'], c['action'], c['direction'])
        assert key not in keys and c['id'] not in ids, key
        keys.add(key); ids.add(c['id'])
        assert abs(sum(f['durationSeconds'] for f in c['frames']) - c['duration']) < .00001
        assert all(0 <= e['timeSeconds'] < c['duration'] for e in c['events'])
    catalog = dict(schemaVersion=1, coordinateSystem='source canvas pixels, origin top-left; UV helper converts Unity atlas origin',
                   clips=sorted(clips, key=lambda c: c['id']))
    write_json(DEST / 'catalog.json', catalog)
    write_json(DEST / 'provenance.json', dict(schemaVersion=1, artworkModified=False, sources=provenance,
                                              anchorEvidence=anchors, excludedDemos=excluded))
    report = dict(ok=True, approvedRuntimeClips=len(items), clipsIncludingAliases=len(clips), uniqueTextures=len(textures),
                  frameEntries=sum(len(c['frames']) for c in clips), sourceHashesVerified=len(items),
                  excludedDemoCount=len(excluded), bytes=sum(p.stat().st_size for p in textures.values()),
                  errors=[], notes=['Original PNG bytes copied exactly; no image editing.',
                                   'Achilles idle/hurt and giant idle reuse existing ready poses.',
                                   'Sarcophagus bare aliases and opposite idle directions share atlases.'])
    write_json(DEST / 'import-report.json', report)
    for path in [DEST, texture_dir]:
        meta_file(path, True)
    for path in DEST.glob('*.json'):
        meta_file(path)
    print(json.dumps(report, ensure_ascii=False))


if __name__ == '__main__':
    main()

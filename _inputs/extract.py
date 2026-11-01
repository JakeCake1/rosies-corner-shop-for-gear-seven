import zipfile, sys, os
z = zipfile.ZipFile(sys.argv[1]); n = 0
for info in z.infolist():
    if info.is_dir(): continue
    p = os.path.normpath(info.filename)
    assert not p.startswith('..') and not os.path.isabs(p)
    os.makedirs(os.path.dirname(p) or '.', exist_ok=True)
    with open(p, 'wb') as f: f.write(z.read(info))
    n += 1
print('extracted', n)

import sys, os, re
root = sys.argv[1]
p = os.path.join(root, 'UnityEngineBridge', 'MainThreadDispatcher.cs')
s = open(p, encoding='utf-8-sig').read()
s = s.replace("""                    if (type == typeof(WWW))
                    {
                        var www = (WWW)current;
                        editorQueueWorker.Enqueue(_ => ConsumeEnumerator(UnwrapWaitWWW(www, routine)), null);
                        return;
                    }""", """                    if (false) // Unity 6.3 removed UnityEngine.WWW (patched for this project)
                    {
                    }""")
s = re.sub(r"(            IEnumerator UnwrapWaitWWW\(WWW www, IEnumerator continuation\)\s*\{.*?\n            \})",
           r"#if UNIRX_LEGACY_WWW\n\1\n#endif", s, count=1, flags=re.S)
open(p, 'w', encoding='utf-8').write(s)
w = os.path.join(root, 'UnityEngineBridge', 'ObservableWWW.cs')
t = open(w, encoding='utf-8-sig').read()
if not t.startswith('#if UNIRX_LEGACY_WWW'):
    open(w, 'w', encoding='utf-8').write('#if UNIRX_LEGACY_WWW // Unity 6.3 removed UnityEngine.WWW (patched for this project)\n' + t + '\n#endif\n')
print('patched', 'WWW' in s.split('#if UNIRX_LEGACY_WWW')[0].replace('typeof(WWW)', ''))

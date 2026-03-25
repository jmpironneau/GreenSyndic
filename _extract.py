import zipfile, xml.etree.ElementTree as ET, sys
sys.stdout.reconfigure(encoding='utf-8')
with zipfile.ZipFile('Additional Data/Docs & Marketing/GreenSyndic_Specifications_v13.docx') as z:
    with z.open('word/document.xml') as f:
        tree = ET.parse(f)
        root = tree.getroot()
        ns = 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'
        texts = []
        for p in root.iter(f'{{{ns}}}p'):
            line = ''.join(t.text or '' for t in p.iter(f'{{{ns}}}t'))
            texts.append(line)
        full = '\n'.join(texts)
        print(full)

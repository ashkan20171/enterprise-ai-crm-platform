from pathlib import Path
import xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[1]
a={}
for culture in ('fa-IR','en-US'):
 r=ET.parse(root/'Resources'/f'SharedResource.{culture}.resx').getroot()
 a[culture]={e.attrib['name'] for e in r.findall('data')}
assert a['fa-IR']==a['en-US']
for page in ('Login','Register','Denied'):
 s=(root/'Views/Auth'/f'{page}.cshtml').read_text()
 assert 'IStringLocalizer' in s and 'dir="@(en ? "ltr" : "rtl")"' in s
assert 'ResourcesPath = "Resources"' in (root/'Program.cs').read_text()
print(f'Stage104 structure OK: {len(a["fa-IR"])} matched resource keys')

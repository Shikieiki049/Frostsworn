"""Use native numeric BBCode colors in every mod relic description.

Color only visible numbers: BBCode parameters (e.g. colors) stay intact.
Healing and Max HP quantities/thresholds use green; other values use blue.
"""
import re
TOKENS=re.compile(r'(\[[^\]]+\]|\{[^{}]+\})')
NUMBERS=re.compile(r'\d+(?:\.\d+)?%?')
def plain(text):return re.sub(r'\[[^\]]+\]','',text)
def color_numbers(text):
 parts=TOKENS.split(text); offset=0; colored=0; result=[]
 for part in parts:
  if part.startswith('['):
   tag=part[1:-1].split('=')[0].lower()
   if tag in ('blue','green','red','color'):colored+=1
   elif tag in ('/blue','/green','/red','/color'):colored=max(0,colored-1)
   result.append(part)
  elif part.startswith('{') or colored:result.append(part)
  else:
   def replace(m):
    before=plain(text[:offset+m.start()]); after=plain(text[offset+m.end():])
    healing=bool(re.search(r'(?:回复|恢复|heal)\s*$',before,re.I))
    maxhp=bool(re.search(r'最大生命值?(?:的|大于|至少为)?\s*$',before) or re.match(r'^\s*(?:点)?最大生命',after) or re.match(r'^\s*(?:(?:of|of your|of the)\s+)?Max HP\b',after,re.I))
    color='green' if healing or maxhp else 'blue'
    return f'[{color}]{m.group()}[/{color}]'
   result.append(NUMBERS.sub(replace,part))
  offset+=len(part)
 return ''.join(result)
def apply(tables):
 for key,text in tables['relics'].items():
  if key.endswith('.description'):tables['relics'][key]=color_numbers(text)

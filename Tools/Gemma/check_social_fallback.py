"""Exercise real local Gemma with the primary evaluator intentionally unavailable."""
import contextlib
import json
from pathlib import Path
import sys
import social_evaluator as social

ROOT=Path(__file__).resolve().parents[2]
profiles=json.loads((ROOT/'Tools/Gemma/Prompts/office_staff_defaults.json').read_text(encoding='utf-8'))['personas']
social.api_key=lambda:''
with contextlib.redirect_stdout(sys.stderr):
    import litert_lm
    litert_lm.set_min_log_severity(litert_lm.LogSeverity.ERROR)
    engine=litert_lm.Engine(str(ROOT/'Models/gemma-4-E2B-it.litertlm'),backend=litert_lm.Backend.CPU(thread_count=4),max_num_tokens=4096)
report=[]
try:
    humour='Гена, ты умеешь разрядить обстановку, с тобой команда не паникует!'
    cases=[
        ('Ты всегда находишь время помочь коллегам, это очень ценно.',[],'positive'),
        ('Ты глупый жмот! Давай деньги немедленно!',[],'negative'),
        ('Ты всегда находишь время помочь коллегам, это очень ценно.',[humour],'positive'),
        ('Ты отлично снимаешь напряжение в коллективе.',[humour],'repeated'),
    ]
    for text,accepted,expected in cases:
        result=social.evaluate_social(engine,litert_lm,dict(persona=profiles[0],text=text,accepted_messages=accepted))
        report.append(dict(expected=expected,result=result));print(json.dumps(report[-1],ensure_ascii=True),flush=True)
        assert result['valid'] and result['backend']=='gemma' and result['outcome']==expected
finally:
    engine.close()
    folder=ROOT/'.local/StaffDialogueQA';folder.mkdir(parents=True,exist_ok=True)
    (folder/'gemma_fallback.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')

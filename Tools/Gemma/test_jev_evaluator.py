import unittest
from unittest.mock import patch
import hiring_evaluator as hiring
from jev_evaluator import parse_jev

class RoutingTests(unittest.TestCase):
    def test_jev_yes_and_no(self):
        for p, expected in [(.92, True), (.04, False), (.7, True), (.3, False)]:
            self.assertEqual(parse_jev({'answers': {'hire': {'type':'noul','noul':p}}})['accepted'], expected)
    def test_bad_responses(self):
        for p in [True,'0.9',None,-1,2,float('nan'),float('inf'),.5]:
            with self.assertRaises((ValueError,TypeError)):
                parse_jev({'answers': {'hire': {'type':'noul','noul':p}}})
        for value in [{}, {'answers':{}}, {'answers':{'hire':{'type':'choice','noul':.9}}}]:
            with self.assertRaises((KeyError,ValueError)):parse_jev(value)
    def test_primary_uses_gemma_for_reply_only(self):
        with patch.object(hiring,'evaluate_jev',return_value={'valid':True,'accepted':True,'backend':'jev','text':'template'}),patch.object(hiring,'generate_reply',return_value={'text':'generated'}) as gemma:
            result=hiring.evaluate_hiring(None,None,'test',[])
            self.assertEqual(result['backend'],'jev');self.assertEqual(result['text'],'generated')
            gemma.assert_called_once_with(None,None,'test',[],True,None)
    def test_reply_failure_keeps_jev_verdict(self):
        with patch.object(hiring,'evaluate_jev',return_value={'valid':True,'accepted':False,'backend':'jev','text':'template'}),patch.object(hiring,'generate_reply',side_effect=RuntimeError()):
            result=hiring.evaluate_hiring(None,None,'test',[])
            self.assertFalse(result['accepted']);self.assertEqual(result['text'],'template')
    def test_errors_fallback(self):
        for error in [TimeoutError(),ValueError(),OSError()]:
            with patch.object(hiring,'evaluate_jev',side_effect=error),patch.object(hiring,'evaluate_gemma',return_value={'valid':True,'accepted':False}) as gemma:
                result=hiring.evaluate_hiring(None,None,'test',[])
                self.assertEqual(result['backend'],'gemma');self.assertFalse(result['accepted']);gemma.assert_called_once()
    def test_both_fail_closed(self):
        with patch.object(hiring,'evaluate_jev',side_effect=ValueError()),patch.object(hiring,'evaluate_gemma',side_effect=RuntimeError()):
            result=hiring.evaluate_hiring(None,None,'test',[]);self.assertFalse(result['valid']);self.assertFalse(result['accepted'])
    def test_streams_sentences_before_generation_finishes(self):
        from types import SimpleNamespace
        from unittest.mock import MagicMock
        seen=[]
        def chunks():
            for fragment in ['Первое ', 'предложение.', ' Второе ', 'предложение!']:
                yield {'content':[{'type':'text','text':fragment}]}
                if fragment==' Второе ':self.assertEqual(seen,['Первое предложение.'])
        conversation=MagicMock();conversation.send_message_async.side_effect=lambda _:chunks()
        engine=MagicMock();engine.create_conversation.return_value=conversation
        runtime=SimpleNamespace(Message=SimpleNamespace(system=lambda s:s),ThinkingConfig=lambda **k:k)
        result=hiring.generate_reply(engine,runtime,'test',[],False,seen.append)
        self.assertEqual(seen,['Первое предложение.','Второе предложение!'])
        self.assertEqual(result['text'],'Первое предложение. Второе предложение!')
        conversation.close.assert_called_once()
if __name__=='__main__':unittest.main()

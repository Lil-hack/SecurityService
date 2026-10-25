import json
import unittest
from unittest.mock import patch
import social_evaluator as social


def response(effect='positive', novel=.99, loan=.01):
    return {'answers': {'effect': {'type':'choice','choice':effect,'confidence':.95,
        'probabilities':{key:.98 if key==effect else .01 for key in social.OUTCOMES}},
        'novel':{'type':'noul','noul':novel},'loan_request':{'type':'noul','noul':loan}}}


class SocialTests(unittest.TestCase):
    def test_positive(self):
        result=social.parse_jev(response());self.assertTrue(result['valid']);self.assertEqual(result['outcome'],'positive');self.assertFalse(result['loan_request'])
    def test_semantic_repetition(self):
        result=social.parse_jev(response(novel=.01));self.assertEqual(result['outcome'],'repeated')
    def test_first_positive_has_nothing_to_repeat(self):
        result=social.parse_jev(response(novel=.01),first_message=True)
        self.assertEqual(result['outcome'],'positive');self.assertTrue(result['novel'])
    def test_negative_does_not_use_uncertain_unused_branches(self):
        result=social.parse_jev(response('negative',.5,.5));self.assertEqual(result['outcome'],'negative');self.assertFalse(result['loan_request'])
    def test_loan(self):
        self.assertTrue(social.parse_jev(response(loan=.99))['loan_request'])
    def test_uncertain_novel_requires_fallback(self):
        with self.assertRaises(ValueError):social.parse_jev(response(novel=.5))
    def test_uncertain_loan_requires_fallback(self):
        with self.assertRaises(ValueError):social.parse_jev(response(loan=.5))
    def test_uncertain_effect(self):
        value=response();value['answers']['effect']['confidence']=.15
        with self.assertRaises(ValueError):social.parse_jev(value)
    def test_bad_distribution(self):
        value=response();value['answers']['effect']['probabilities']['positive']=float('nan')
        with self.assertRaises(ValueError):social.parse_jev(value)
    def test_bool_probability_invalid(self):
        with self.assertRaises(ValueError):social.parse_jev(response(loan=True))
    def test_local_parse(self):
        result=social.parse_local('```json\n{"outcome":"positive","novel":true,"loan_request":true}\n```')
        self.assertTrue(result['valid']);self.assertEqual(result['backend'],'gemma')
    def test_local_cannot_assign_trust(self):
        result=social.parse_local('{"outcome":"positive","novel":true,"loan_request":false,"trust":100,"payment":9999}')
        self.assertNotIn('trust',result);self.assertNotIn('payment',result)
    def test_malformed_local_no_effect(self):
        for value in ['no','{"outcome":"positive","novel":1,"loan_request":true}','{"outcome":"accepted","novel":true,"loan_request":true}']:
            self.assertFalse(social.parse_local(value)['valid'])
    def test_exact_duplicate_bypasses_inference(self):
        result=social.evaluate_social(None,None,{'text':'  Ты отлично помогаешь!','accepted_messages':['ты отлично помогаешь.']})
        self.assertEqual(result['outcome'],'repeated');self.assertEqual(result['backend'],'duplicate')
    def test_primary_wins(self):
        with patch.object(social,'evaluate_jev_social',return_value=social.parse_jev(response())),patch.object(social,'collect',side_effect=AssertionError('Fallback should not run')):
            self.assertEqual(social.evaluate_social(None,None,{'text':'Новый комплимент'})['backend'],'jev')
    def test_fallback(self):
        with patch.object(social,'evaluate_jev_social',side_effect=ValueError('uncertain')),patch.object(social,'collect',return_value='{"outcome":"negative","novel":false,"loan_request":false}'):
            self.assertEqual(social.evaluate_social(None,None,{'text':'Грубость'})['backend'],'gemma')
    def test_first_positive_fallback_normalizes_empty_history(self):
        with patch.object(social,'evaluate_jev_social',side_effect=ValueError('unavailable')),patch.object(social,'collect',return_value='{"outcome":"positive","novel":false,"loan_request":false}'):
            result=social.evaluate_social(None,None,{'text':'Ты очень внимательный коллега'})
            self.assertEqual(result['outcome'],'positive');self.assertTrue(result['novel'])
    def test_fallback_compares_meaning_separately(self):
        for novel in (True,False):
            with self.subTest(novel=novel),patch.object(social,'evaluate_jev_social',side_effect=ValueError('unavailable')),patch.object(social,'collect',side_effect=['{"outcome":"positive","novel":true,"loan_request":false}',json.dumps({'new_reason':novel})]):
                result=social.evaluate_social(None,None,{'text':'Новый довод','accepted_messages':['Старый довод']})
                self.assertEqual(result['outcome'],'positive' if novel else 'repeated')
    def test_malformed_novelty_does_not_award(self):
        with patch.object(social,'evaluate_jev_social',side_effect=ValueError('unavailable')),patch.object(social,'collect',side_effect=['{"outcome":"positive","novel":true,"loan_request":false}','{"new_reason":"yes"}']):
            result=social.evaluate_social(None,None,{'text':'Новый довод','accepted_messages':['Старый довод']})
            self.assertFalse(result['valid'])
    def test_unavailable_does_not_change_game(self):
        with patch.object(social,'evaluate_jev_social',side_effect=OSError()),patch.object(social,'collect',side_effect=OSError()):
            result=social.evaluate_social(None,None,{'text':'Текст'});self.assertFalse(result['valid']);self.assertEqual(result['backend'],'unavailable')
    def test_bounded_state(self):
        state=social.clean_state({'text':'x'*4000,'accepted_messages':['x'*500]*40,'history':[{'role':'user','text':'y'*500}]*40})
        self.assertEqual(len(state['player_message']),1000);self.assertEqual(len(state['accepted_messages']),12);self.assertEqual(len(state['previous_dialogue']),4)


if __name__=='__main__':unittest.main()

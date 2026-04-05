import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import ExplanationPanel from '../src/components/ExplanationPanel.vue';
import type { Analysis } from '../src/types/chess';

const createAnalysis = (overrides: Partial<Analysis['explanation']> = {}): Analysis => ({
  id: 'analysis-1',
  moveNumber: 1,
  side: 'w',
  fen: 'test-fen',
  move: 'e4',
  explanation: {
    moveQuality: 'good',
    whyItWasPlayed: 'Control the center',
    strategicIdeas: ['Center control', 'Development'],
    alternatives: [],
    whatToConsider: 'Watch for counterattack',
    evaluation: 0.5,
    ...overrides
  }
});

describe('ExplanationPanel', () => {
  it('renders empty state when no analysis', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { analysis: null }
    });
    
    expect(wrapper.find('.explanation-panel.empty').exists()).toBe(true);
    expect(wrapper.find('.explanation-panel.empty').text()).toBe('Select a move to see the analysis');
  });

  it('renders quality badge with correct class', () => {
    const analysis = createAnalysis({ moveQuality: 'excellent' });
    const wrapper = mount(ExplanationPanel, {
      props: { analysis }
    });
    
    const badge = wrapper.find('.quality-badge');
    expect(badge.exists()).toBe(true);
    expect(badge.text()).toBe('excellent');
    expect(badge.classes()).toContain('excellent');
  });

  it('renders good quality badge correctly', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { analysis: createAnalysis({ moveQuality: 'good' }) }
    });
    
    expect(wrapper.find('.quality-badge.good').exists()).toBe(true);
  });

  it('renders inaccuracy quality badge correctly', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { analysis: createAnalysis({ moveQuality: 'inaccuracy' }) }
    });
    
    expect(wrapper.find('.quality-badge.inaccuracy').exists()).toBe(true);
  });

  it('renders mistake quality badge correctly', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { analysis: createAnalysis({ moveQuality: 'mistake' }) }
    });
    
    expect(wrapper.find('.quality-badge.mistake').exists()).toBe(true);
  });

  it('renders blunder quality badge correctly', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { analysis: createAnalysis({ moveQuality: 'blunder' }) }
    });
    
    expect(wrapper.find('.quality-badge.blunder').exists()).toBe(true);
  });

  it('renders why section', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { analysis: createAnalysis({ whyItWasPlayed: 'To control the center' }) }
    });
    
    expect(wrapper.find('.section h3').text()).toBe('Why this move?');
    expect(wrapper.find('.section p').text()).toBe('To control the center');
  });

  it('renders strategic ideas section when present', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { 
        analysis: createAnalysis({ 
          strategicIdeas: ['Center control', 'King safety'] 
        }) 
      }
    });
    
    const sections = wrapper.findAll('.section');
    const strategicSection = sections.find(s => s.find('h3').text() === 'Strategic Ideas');
    expect(strategicSection?.exists()).toBe(true);
    const tags = strategicSection?.findAll('.tag');
    expect(tags).toHaveLength(2);
    expect(tags?.[0].text()).toBe('Center control');
    expect(tags?.[1].text()).toBe('King safety');
  });

  it('does not render strategic ideas section when empty', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { 
        analysis: createAnalysis({ strategicIdeas: [] }) 
      }
    });
    
    expect(wrapper.text()).not.toContain('Strategic Ideas');
  });

  it('renders alternatives section when present', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { 
        analysis: createAnalysis({ 
          alternatives: [
            { move: 'd4', reason: 'More solid', evaluation: 0.3 },
            { move: 'c4', reason: 'Flank opening', evaluation: -0.1 }
          ]
        }) 
      }
    });
    
    const sections = wrapper.findAll('.section');
    const altSection = sections.find(s => s.find('h3').text() === 'Alternatives to consider');
    expect(altSection?.exists()).toBe(true);
    const alternatives = altSection?.findAll('.alternative');
    expect(alternatives).toHaveLength(2);
    expect(alternatives?.[0].find('.alt-move').text()).toBe('d4');
    expect(alternatives?.[0].find('.alt-reason').text()).toBe('More solid');
  });

  it('does not render alternatives section when empty', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { 
        analysis: createAnalysis({ alternatives: [] }) 
      }
    });
    
    expect(wrapper.text()).not.toContain('Alternatives to consider');
  });

  it('renders what to consider section when present', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { 
        analysis: createAnalysis({ whatToConsider: 'Watch for tactics' }) 
      }
    });
    
    const sections = wrapper.findAll('.section');
    const considerSection = sections.find(s => s.find('h3').text() === 'What to consider');
    expect(considerSection?.exists()).toBe(true);
    expect(considerSection?.find('.consideration').text()).toBe('Watch for tactics');
  });

  it('renders evaluation section when present', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { 
        analysis: createAnalysis({ evaluation: 0.5 }) 
      }
    });
    
    const evalSection = wrapper.find('.section.evaluation');
    expect(evalSection.exists()).toBe(true);
    expect(evalSection.text()).toContain('Evaluation:');
  });

  it('formats positive evaluation correctly', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { 
        analysis: createAnalysis({ evaluation: 50 }) 
      }
    });
    
    expect(wrapper.find('.eval-value').text()).toBe('+5');
  });

  it('formats negative evaluation correctly', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { 
        analysis: createAnalysis({ evaluation: -30 }) 
      }
    });
    
    expect(wrapper.find('.eval-value').text()).toBe('-3');
  });

  it('formats zero evaluation correctly', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { 
        analysis: createAnalysis({ evaluation: 0 }) 
      }
    });
    
    expect(wrapper.find('.eval-value').text()).toBe('0');
  });

  it('renders evaluation section when evaluation is undefined', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { 
        analysis: createAnalysis({ evaluation: undefined }) 
      }
    });
    
    const evalSection = wrapper.find('.section.evaluation');
    expect(evalSection.exists()).toBe(false);
  });

  it('applies good evaluation class for high positive', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { 
        analysis: createAnalysis({ evaluation: 50 }) 
      }
    });
    
    expect(wrapper.find('.eval-value.good').exists()).toBe(true);
  });

  it('applies bad evaluation class for high negative', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { 
        analysis: createAnalysis({ evaluation: -50 }) 
      }
    });
    
    expect(wrapper.find('.eval-value.bad').exists()).toBe(true);
  });

  it('applies neutral evaluation class for small values', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { 
        analysis: createAnalysis({ evaluation: 10 }) 
      }
    });
    
    expect(wrapper.find('.eval-value.neutral').exists()).toBe(true);
  });

  it('applies good class to positive alternative evaluation', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { 
        analysis: createAnalysis({ 
          alternatives: [{ move: 'd4', reason: 'Good', evaluation: 40 }]
        }) 
      }
    });
    
    expect(wrapper.find('.alt-eval.good').exists()).toBe(true);
  });

  it('applies bad class to negative alternative evaluation', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { 
        analysis: createAnalysis({ 
          alternatives: [{ move: 'd4', reason: 'Bad', evaluation: -40 }]
        }) 
      }
    });
    
    expect(wrapper.find('.alt-eval.bad').exists()).toBe(true);
  });

  it('renders unknown quality badge for unknown quality', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { 
        analysis: createAnalysis({ moveQuality: 'unknown' }) 
      }
    });
    
    expect(wrapper.find('.quality-badge.unknown').exists()).toBe(true);
  });

  it('renders all sections when fully populated', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { 
        analysis: createAnalysis({
          moveQuality: 'good',
          whyItWasPlayed: 'Center control',
          strategicIdeas: ['Development'],
          alternatives: [{ move: 'd4', reason: 'Solid', evaluation: 0.2 }],
          whatToConsider: 'King safety',
          evaluation: 0.5
        })
      }
    });
    
    expect(wrapper.find('.quality-badge').exists()).toBe(true);
    expect(wrapper.text()).toContain('Why this move?');
    expect(wrapper.text()).toContain('Strategic Ideas');
    expect(wrapper.text()).toContain('Alternatives to consider');
    expect(wrapper.text()).toContain('What to consider');
    expect(wrapper.text()).toContain('Evaluation:');
  });

  it('handles empty string for whatToConsider', () => {
    const wrapper = mount(ExplanationPanel, {
      props: { 
        analysis: createAnalysis({ whatToConsider: '' }) 
      }
    });
    
    expect(wrapper.text()).not.toContain('What to consider');
  });
});
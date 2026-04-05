import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import GameImporter from '../src/components/GameImporter.vue';
import { gamesApi } from '../src/services/api';

vi.mock('../src/services/api', () => ({
  gamesApi: {
    import: vi.fn()
  }
}));

vi.mock('vue-router', () => ({
  useRouter: () => ({
    push: vi.fn()
  })
}));

describe('GameImporter', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders drag and drop area', () => {
    const wrapper = mount(GameImporter);
    expect(wrapper.find('.import-area').exists()).toBe(true);
  });

  it('renders textarea placeholder', () => {
    const wrapper = mount(GameImporter);
    const textarea = wrapper.find('textarea');
    expect(textarea.exists()).toBe(true);
    expect(textarea.attributes('placeholder')).toBe('Paste PGN here...');
  });

  it('shows drag styling when dragging', async () => {
    const wrapper = mount(GameImporter);
    
    await wrapper.find('.import-area').trigger('dragover');
    
    expect(wrapper.find('.import-area.dragging').exists()).toBe(true);
  });

  it('removes drag styling on dragleave', async () => {
    const wrapper = mount(GameImporter);
    
    await wrapper.find('.import-area').trigger('dragover');
    await wrapper.find('.import-area').trigger('dragleave');
    
    expect(wrapper.find('.import-area.dragging').exists()).toBe(false);
  });

  it('shows preview when PGN has header info', async () => {
    const pgn = `[Event "Test"]
[White "Player1"]
[Black "Player2"]
[Result "1-0"]

1. e4 e5 2. Nf3`;
    
    const wrapper = mount(GameImporter);
    const textarea = wrapper.find('textarea');
    
    await textarea.setValue(pgn);
    await wrapper.vm.$nextTick();
    
    expect(wrapper.find('.preview').exists()).toBe(true);
    expect(wrapper.find('.preview').text()).toContain('Player1');
    expect(wrapper.find('.preview').text()).toContain('Player2');
  });

  it('shows import button when game is parsed with moves', async () => {
    const pgn = `[White "Player1"]
[Black "Player2"]

1. e4 e5`;
    
    const wrapper = mount(GameImporter);
    const textarea = wrapper.find('textarea');
    
    await textarea.setValue(pgn);
    await wrapper.vm.$nextTick();
    
    expect(wrapper.find('.import-btn').exists()).toBe(true);
  });

  it('hides import button when no moves', () => {
    const wrapper = mount(GameImporter);
    
    expect(wrapper.find('.import-btn').exists()).toBe(false);
  });

  it('import button is disabled when importing', async () => {
    const mockResponse = { data: { id: 'game-123' } };
    vi.mocked(gamesApi.import).mockImplementation(() => 
      new Promise((_, reject) => setTimeout(() => reject(mockResponse), 100))
    );

    const pgn = `[White "Player1"]
[Black "Player2"]
1. e4 e5`;

    const wrapper = mount(GameImporter);
    const textarea = wrapper.find('textarea');
    
    await textarea.setValue(pgn);
    await wrapper.find('.import-btn').trigger('click');
    await wrapper.vm.$nextTick();
    
    const btn = wrapper.find('.import-btn');
    expect(btn.attributes('disabled')).toBeDefined();
    expect(btn.text()).toBe('Importing...');
  });

  it('calls import API when import button is clicked', async () => {
    const mockResponse = { data: { id: 'game-123' } };
    vi.mocked(gamesApi.import).mockResolvedValue(mockResponse);

    const pgn = `[White "Player1"]
[Black "Player2"]
1. e4 e5`;

    const wrapper = mount(GameImporter);
    const textarea = wrapper.find('textarea');
    
    await textarea.setValue(pgn);
    await wrapper.find('.import-btn').trigger('click');
    
    expect(gamesApi.import).toHaveBeenCalledWith(pgn);
  });

  it('emits imported event after successful import', async () => {
    const mockResponse = { data: { id: 'game-123' } };
    vi.mocked(gamesApi.import).mockResolvedValue(mockResponse);

    const pgn = `[White "Player1"]
[Black "Player2"]
1. e4 e5`;

    const wrapper = mount(GameImporter, {
      emit: ['imported']
    });
    
    const textarea = wrapper.find('textarea');
    await textarea.setValue(pgn);
    await wrapper.find('.import-btn').trigger('click');
    await wrapper.vm.$nextTick();
    
    expect(wrapper.emitted('imported')).toBeTruthy();
    expect(wrapper.emitted('imported')?.[0]).toEqual(['game-123']);
  });

  it('shows error on import failure', async () => {
    const mockError = { response: { data: { message: 'Import failed' } } };
    vi.mocked(gamesApi.import).mockRejectedValue(mockError);

    const pgn = `[White "Player1"]
[Black "Player2"]
1. e4 e5`;

    const wrapper = mount(GameImporter);
    
    const textarea = wrapper.find('textarea');
    await textarea.setValue(pgn);
    await wrapper.find('.import-btn').trigger('click');
    await wrapper.vm.$nextTick();
    
    expect(wrapper.find('.error').exists()).toBe(true);
    expect(wrapper.find('.error').text()).toBe('Import failed');
  });

  it('clears game on clear button click', async () => {
    const pgn = `1. e4 e5`;

    const wrapper = mount(GameImporter);
    
    const textarea = wrapper.find('textarea');
    await textarea.setValue(pgn);
    await wrapper.vm.$nextTick();
    
    expect(wrapper.find('.preview').exists()).toBe(true);
    
    await wrapper.find('.clear-btn').trigger('click');
    await wrapper.vm.$nextTick();
    
    expect(wrapper.find('.preview').exists()).toBe(false);
    expect(wrapper.find('textarea').element.value).toBe('');
  });

  it('does not show preview when text is empty', () => {
    const wrapper = mount(GameImporter);
    
    expect(wrapper.find('.preview').exists()).toBe(false);
  });

  it('displays "or" text between instructions', () => {
    const wrapper = mount(GameImporter);
    
    expect(wrapper.find('.or').exists()).toBe(true);
    expect(wrapper.find('.or').text()).toBe('or');
  });

  it('displays "Unknown" for missing header tags', async () => {
    const pgn = `1. e4 e5`;
    
    const wrapper = mount(GameImporter);
    const textarea = wrapper.find('textarea');
    
    await textarea.setValue(pgn);
    await wrapper.vm.$nextTick();
    
    expect(wrapper.find('.preview').text()).toContain('Unknown');
  });

  it('handles whitespace-only input', async () => {
    const wrapper = mount(GameImporter);
    const textarea = wrapper.find('textarea');
    
    await textarea.setValue('   ');
    await wrapper.vm.$nextTick();
    
    expect(wrapper.find('.preview').exists()).toBe(false);
  });

  it('displays move count in preview', async () => {
    const pgn = `[White "Player1"]
[Black "Player2"]
1. e4 e5 2. d4`;
    
    const wrapper = mount(GameImporter);
    const textarea = wrapper.find('textarea');
    
    await textarea.setValue(pgn);
    await wrapper.vm.$nextTick();
    
    expect(wrapper.find('.preview').text()).toContain('Moves:');
  });
});
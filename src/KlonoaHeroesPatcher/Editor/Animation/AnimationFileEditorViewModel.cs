using System.Collections.ObjectModel;
using BinarySerializer.Klonoa.KH;
using System.Linq;

namespace KlonoaHeroesPatcher
{
    public class AnimationFileEditorViewModel : FileEditorViewModel
    {
        public Animation_File AnimationFile => (Animation_File)SerializableObject;

        public ObservableCollection<AnimationViewModel> Animations { get; set; }
        public AnimationViewModel SelectedAnimation { get; set; }

        public override void Load(bool firstLoad)
        {
            Animations = new ObservableCollection<AnimationViewModel>();

            for (int groupIndex = 0; groupIndex < AnimationFile.AnimationGroups.Length; groupIndex++)
            {
                AnimationGroup group = AnimationFile.AnimationGroups[groupIndex];

                for (int animIndex = 0; animIndex < group.Animations.Length; animIndex++)
                {
                    Animation anim = group.Animations[animIndex];

                    if (anim == null)
                        continue;

                    if (Animations.Any(x => x.Animation == anim))
                        continue;

                    Animations.Add(new AnimationViewModel(AnimationFile, groupIndex, animIndex));
                }
            }

            SelectedAnimation = Animations.FirstOrDefault();
            SelectedAnimation?.RefreshGIF();
        }

        public override void Unload()
        {
            if (Animations != null)
                foreach (AnimationViewModel anim in Animations)
                    anim?.Dispose();

            Animations = null;
            SelectedAnimation = null;
        }
    }
}
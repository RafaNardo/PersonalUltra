import { Image, type ImageProps, type ImageSource } from 'expo-image';
import { useEffect, useState } from 'react';
import { StyleSheet, Text, View, type StyleProp, type ViewStyle } from 'react-native';
import { colors, typography } from '@/src/design/tokens';
import { exerciseMediaSource } from './exercise-media';

type ExerciseImageProps = {
  imageRef?: string;
  imageUrl?: string;
  style?: ImageProps['style'];
  contentFit?: ImageProps['contentFit'];
  accessibilityLabel?: string;
  accessible?: boolean;
};

export function ExerciseImage({
  imageRef,
  imageUrl,
  style,
  contentFit = 'contain',
  accessibilityLabel,
  accessible,
}: ExerciseImageProps) {
  const source = exerciseMediaSource(imageRef, imageUrl);
  const [failed, setFailed] = useState(false);
  const [cachedSource, setCachedSource] = useState<ImageSource>();

  useEffect(() => {
    let current = true;
    setFailed(false);
    setCachedSource(undefined);
    if (!source && imageRef) {
      void cachedImageSource(imageRef).then((cached) => {
        if (current) setCachedSource(cached);
      });
    }
    return () => { current = false; };
  }, [imageRef, imageUrl]);

  const displaySource = cachedSource ?? source;
  const handleError = () => {
    if (cachedSource) { setFailed(true); return; }
    if (!imageRef) { setFailed(true); return; }
    void cachedImageSource(imageRef).then((cached) => {
      if (cached) setCachedSource(cached);
      else setFailed(true);
    });
  };

  return <View
    accessible={accessible ?? Boolean(accessibilityLabel)}
    accessibilityRole="image"
    accessibilityLabel={displaySource && !failed ? accessibilityLabel : 'Imagem indisponível'}
    style={[styles.frame, style as StyleProp<ViewStyle>]}
  >
    {displaySource && !failed
      ? <Image
          source={displaySource}
          cachePolicy="disk"
          contentFit={contentFit}
          recyclingKey={imageRef ?? null}
          accessible={false}
          onError={handleError}
          style={StyleSheet.absoluteFill}
        />
      : <View style={styles.placeholder}><Text accessibilityElementsHidden style={styles.placeholderText}>PU</Text></View>}
  </View>;
}

async function cachedImageSource(imageRef: string): Promise<ImageSource | undefined> {
  const cacheKey = imageRef.trim().replace(/^\/+/, '');
  if (!cacheKey) return undefined;
  const path = await Image.getCachePathAsync(cacheKey).catch(() => null);
  if (!path) return undefined;
  return { uri: path.startsWith('file://') ? path : `file://${path}`, cacheKey };
}

const styles = StyleSheet.create({
  frame: { overflow: 'hidden', backgroundColor: colors.surfaceElevated },
  placeholder: { ...StyleSheet.absoluteFillObject, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.surfaceElevated },
  placeholderText: { ...typography.headingMD, color: colors.textMuted, letterSpacing: 1 },
});

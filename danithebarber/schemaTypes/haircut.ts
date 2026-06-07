import { defineType, defineField } from 'sanity'

export const haircut = defineType({
  name: 'haircut',
  title: 'Catálogo de Cortes',
  type: 'document',
  fields: [
    defineField({
      name: 'title',
      title: 'Título del Corte',
      type: 'string',
      description: 'Ej: Mid Fade Texturizado, Slick Back, Mullet Urbano...',
      validation: (Rule) => Rule.required().min(3).max(50),
    }),
    defineField({
      name: 'description',
      title: 'Descripción',
      type: 'text',
      rows: 3,
      description: 'Explica brevemente la técnica o el acabado del corte.',
      validation: (Rule) => Rule.required().max(200),
    }),
    defineField({
      name: 'category',
      title: 'Categoría',
      type: 'string',
      description: 'Selecciona la categoría para que funcione el filtro en la web.',
      options: {
        list: [
          { title: 'Degradados', value: 'degradados' },
          { title: 'Barbas', value: 'barbas' },
          { title: 'Diseños', value: 'diseños' },
          { title: 'Otros', value: 'otros' },
        ],
        layout: 'radio',
      },
      validation: (Rule) => Rule.required(),
    }),
    defineField({
      name: 'image',
      title: 'Foto del Resultado (Después)',
      type: 'image',
      options: {
        hotspot: true,
      },
      validation: (Rule) => Rule.required(),
    }),
    defineField({
      name: 'beforeImage',
      title: 'Foto del Antes (Opcional)',
      type: 'image',
      options: {
        hotspot: true,
      },
      description: 'Si subes una foto aquí, la web activará automáticamente el efecto interactivo Antes/Después.',
    }),
  ],
  preview: {
    select: {
      title: 'title',
      subtitle: 'category',
      media: 'image',
    },
  },
})
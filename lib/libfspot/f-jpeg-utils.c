/* -*- Mode: C; indent-tabs-mode: t; c-basic-offset: 8; tab-width: 8 -*-
 *
 * f-jpeg-utils.c: Utility functions for JPEG files.
 * 
 * Copyright (C) 2001 Red Hat Inc.
 * Copyright (C) 2001 The Free Software Foundation, Inc.
 * Copyright (C) 2003 Ettore Perazzoli
 *  
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 2 of the
 * License, or (at your option) any later version.
 *  
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *  
 * You should have received a copy of the GNU General Public
 * License along with this program; if not, write to the
 * Free Software Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 *  
 * Authors: Alexander Larsson <alexl@redhat.com>
 *          Ettore Perazzoli <ettore@perazzoli.org>
 *          Paolo Bacchilega <paolo.bacch@tin.it>
 */

#include <config.h>

#include "f-jpeg-utils.h"

#include <setjmp.h>
#include <stdio.h>

/* libjpeg likes to define these symbols, that we already define in config.h.  */
#undef HAVE_STDDEF_H
#undef HAVE_STDLIB_H
/* HAVE_LIBEXIF is required by jpeg-data.h.  */
#define HAVE_LIBEXIF
#include "libjpegtran/jpeg-data.h"
#include "libjpegtran/jpegtran.h"

#include <glib.h>
#include <gio/gio.h>

#include <libexif/exif-data.h>
#include <libexif/exif-content.h>
#include <libexif/exif-entry.h>

#define BUFFER_SIZE 8192

/* FIME these strings should actually be translated */
#define _(x) x


typedef struct {
	struct jpeg_source_mgr pub;	/* public fields */
	GInputStream *stream;
	JOCTET buffer[BUFFER_SIZE];
} Source;

typedef struct {
	struct jpeg_error_mgr pub;
	jmp_buf setjmp_buffer;
} ErrorHandlerData;

static void
fatal_error_handler (j_common_ptr cinfo)
{
	ErrorHandlerData *data;

	data = (ErrorHandlerData *) cinfo->err;
	longjmp (data->setjmp_buffer, 1);
}

static void
output_message_handler (j_common_ptr cinfo)
{
	/* If we don't supply this handler, libjpeg reports errors
	 * directly to stderr.
	 */
}

static void
init_source (j_decompress_ptr cinfo)
{
}

static gboolean
fill_input_buffer (j_decompress_ptr cinfo)
{
	Source *src;
	gssize nbytes;
	GError *err = NULL;
	
	src = (Source *) cinfo->src;
	nbytes = g_input_stream_read (src->stream,
				      src->buffer,
				      G_N_ELEMENTS (src->buffer),
				      NULL,
				      &err);
	
	if (err != NULL || nbytes == 0) {
		/* return a fake EOI marker so we will eventually terminate */
		src->buffer[0] = (JOCTET) 0xFF;
		src->buffer[1] = (JOCTET) JPEG_EOI;
		nbytes = 2;
	}
	
	src->pub.next_input_byte = src->buffer;
	src->pub.bytes_in_buffer = nbytes;
	
	return TRUE;
}

static void
skip_input_data (j_decompress_ptr cinfo, long num_bytes)
{
	Source *src;

	src = (Source *) cinfo->src;
	if (num_bytes > 0) {
		while (num_bytes > (long) src->pub.bytes_in_buffer) {
			num_bytes -= (long) src->pub.bytes_in_buffer;
			fill_input_buffer (cinfo);
		}
		src->pub.next_input_byte += (size_t) num_bytes;
		src->pub.bytes_in_buffer -= (size_t) num_bytes;
	}
}

static void
term_source (j_decompress_ptr cinfo)
{
}

static void
gio_src (j_decompress_ptr cinfo, GInputStream *stream)
{
	Source *src;
	
	if (cinfo->src == NULL) {	/* first time for this JPEG object? */
		cinfo->src = &(g_new (Source, 1))->pub;
	}

	src = (Source *) cinfo->src;
	src->pub.init_source = init_source;
	src->pub.fill_input_buffer = fill_input_buffer;
	src->pub.skip_input_data = skip_input_data;
	src->pub.resync_to_restart = jpeg_resync_to_restart; /* use default method */
	src->pub.term_source = term_source;
	src->stream = stream;
	src->pub.bytes_in_buffer = 0; /* forces fill_input_buffer on first read */
	src->pub.next_input_byte = NULL; /* until buffer loaded */
}

static void
gio_src_free (j_decompress_ptr cinfo)
{
	g_free (cinfo->src);
}

static int
calculate_divisor (int width,
		   int height,
		   int target_width,
		   int target_height)
{
	if (width/8 > target_width && height/8 > target_height) {
		return 8;
	}
	if (width/4 > target_width && height/4 > target_height) {
		return 4;
	}
	if (width/2 > target_width && height/2 > target_height) {
		return 2;
	}
	return 1;
}

static void
free_buffer (guchar *pixels, gpointer data)
{
	g_free (pixels);
}

/* Implementation of non-lossy JPEG file transformations, based on GThumb code
   by Paolo Bacchilega.  */

static gboolean
swap_fields (ExifContent *content,
	     ExifTag      tag1,
	     ExifTag      tag2)
{
	ExifEntry     *entry1;
	ExifEntry     *entry2;
	unsigned char *data;
	unsigned int   size;
	
	entry1 = exif_content_get_entry (content, tag1);
	if (entry1 == NULL) 
		return FALSE;
	
	entry2 = exif_content_get_entry (content, tag2);
	if (entry2 == NULL)
		return FALSE;

	data = entry1->data;
	size = entry1->size;
	
	entry1->data = entry2->data;
	entry1->size = entry2->size;
	
	entry2->data = data;
	entry2->size = size;

	return TRUE;
}


static void
swap_xy_exif_fields (const char *filename)
{
	JPEGData     *jdata;
	ExifData     *edata;
	unsigned int  i;

	jdata = jpeg_data_new_from_file (filename);
	if (jdata == NULL)
		return;

	edata = jpeg_data_get_exif_data (jdata);
	if (edata == NULL) {
		jpeg_data_unref (jdata);
		return;
	}

	for (i = 0; i < EXIF_IFD_COUNT; i++) {
		ExifContent *content = edata->ifd[i];

		if ((content == NULL) || (content->count == 0)) 
			continue;
		
		swap_fields (content, 
			     EXIF_TAG_RELATED_IMAGE_WIDTH,
			     EXIF_TAG_RELATED_IMAGE_LENGTH);
		swap_fields (content, 
			     EXIF_TAG_IMAGE_WIDTH,
			     EXIF_TAG_IMAGE_LENGTH);
		swap_fields (content, 
			     EXIF_TAG_PIXEL_X_DIMENSION,
			     EXIF_TAG_PIXEL_Y_DIMENSION);
		swap_fields (content, 
			     EXIF_TAG_X_RESOLUTION,
			     EXIF_TAG_Y_RESOLUTION);
		swap_fields (content, 
			     EXIF_TAG_FOCAL_PLANE_X_RESOLUTION,
			     EXIF_TAG_FOCAL_PLANE_Y_RESOLUTION);
	}

	jpeg_data_save_file (jdata, filename);

	exif_data_unref (edata);
	jpeg_data_unref (jdata);
}

gboolean
f_transform_jpeg  (const char      *source_path,
		   const char      *destination_path,
		   FJpegTransform   transform,
		   char           **error_message_return)
{
	JXFORM_CODE jpegtran_transform;
	*error_message_return = NULL;

	/* Since the error reporting of jpegtran sucks, check at least that the source
	   file exists.  */
	if (! g_file_test (source_path, G_FILE_TEST_EXISTS)) {
		if (error_message_return != NULL)
			*error_message_return = g_strdup (_("File not found"));
		return FALSE;
	}

	switch (transform) {
	case F_JPEG_TRANSFORM_ROTATE_90:
		jpegtran_transform = JXFORM_ROT_90;
		break;
	case F_JPEG_TRANSFORM_ROTATE_180:
		jpegtran_transform = JXFORM_ROT_180;
		break;
	case F_JPEG_TRANSFORM_ROTATE_270:
		jpegtran_transform = JXFORM_ROT_270;
		break;
	case F_JPEG_TRANSFORM_FLIP_H:
		jpegtran_transform = JXFORM_FLIP_H;
		break;
	case F_JPEG_TRANSFORM_FLIP_V:
		jpegtran_transform = JXFORM_FLIP_V;
		break;
	default:
		g_warning ("%s(): unknown transform type %d", G_STRFUNC, transform);
		if (error_message_return != NULL)
			*error_message_return = g_strdup_printf (_("Unknown transform type %d"), transform);
		return FALSE;
	}

	/* FIXME: Need to improve the error reporting here.  */
	/* Also note how the jpegtran() args are not const-safe.  */
	if (jpegtran ((char *) source_path, (char *) destination_path, jpegtran_transform) != 0) {
		if (error_message_return != NULL)
			*error_message_return = g_strdup (_("Operation failed"));
		return FALSE;
	}

	/* FIXME: ...And here we have no error reporting at all.  Yuck.  */
	if (transform == F_JPEG_TRANSFORM_ROTATE_270
	    || transform == F_JPEG_TRANSFORM_ROTATE_90)
		swap_xy_exif_fields (destination_path);

	return TRUE;
}
